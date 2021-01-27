using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SpeedAsm
{
    public class Parser
    {
        public static readonly Regex ThreeOperand = new Regex(@"\s*([\w\d_]+)\s*([^\s\w\d_]+)\s*([\w\d_]+)\s*([^\s\w\d_]+)\s*([\w\d_]+)\s*");
        public static readonly Regex TwoOperand = new Regex(@"\s*([\w\d_]+)\s*([^\s\w\d_]+)\s*([\w\d_]+)\s*");
        public static readonly Regex OneOperand = new Regex(@"\s*([\w\d_]+)\s*([^\s\w\d_]+)\s*");
        public static readonly Regex KeywordOperand = new Regex(@"\s*([\w\d_]+)\s+([\w\d_]+)\s*");
        public static readonly Regex Keyword = new Regex(@"\s*([\w\d_]+)\s*");

        private const string Comment = "//";

        private static readonly string Assignment = Operation.Set.GetString();

        public ICollection<ILinePreparser> LinePreparsers { get; } = new List<ILinePreparser>();
        public ICollection<IPostProcess> PostProcesses { get; set; } = new List<IPostProcess>();

        private readonly Dictionary<string, ulong> Labels = new Dictionary<string, ulong>();
        private readonly Dictionary<string, ulong> Variables = new Dictionary<string, ulong>();
        private ulong NextLabel = 0;
        private ulong NextVariable = 0;

        public IEnumerable<Instruction> Parse(IEnumerable<string> lines)
        {
            foreach (var parser in LinePreparsers)
            {
                parser.Begin(this);
            }

            foreach (var post in PostProcesses)
            {
                post.Begin(this);
            }

            var lineIndex = 0;

            foreach (var line in lines)
            {
                var generated = ParseLine(line);

                foreach (var inst in generated)
                {
                    inst.Line = lineIndex;

                    IEnumerable<Instruction> result = new[] { inst };

                    foreach (var post in PostProcesses)
                    {
                        var buffer = new List<Instruction>();

                        foreach (var subInst in result)
                        {
                            buffer.AddRange(post.Process(this, subInst));
                        }

                        result = buffer;
                    }

                    foreach (var postInst in result)
                    {
                        postInst.Line = lineIndex;

                        foreach (var parser in LinePreparsers)
                        {
                            parser.Generated(this, postInst);
                        }

                        foreach (var post in PostProcesses)
                        {
                            post.Generated(this, postInst);
                        }

                        yield return postInst;
                    }
                }

                lineIndex++;
            }

            foreach (var parser in LinePreparsers)
            {
                parser.End(this);
            }

            foreach (var post in PostProcesses)
            {
                post.End(this);
            }
        }

        public IEnumerable<Instruction> ParseLine(string line)
        {
            foreach (var parser in LinePreparsers)
            {
                if (parser.TryParse(this, line, out IEnumerable<Instruction> result))
                {
                    return result;
                }
            }

            var inst = ParseLexResult(Lex(line));

            return inst != null ? new[] { inst } : new Instruction[0];
        }

        public Instruction ParseLexResult(LexResult lex)
        {
            if (lex.Valid)
            {
                if (lex.ThreeOperand && !lex.Error)
                {
                    if (lex.OperatorA == Assignment)
                    {
                        foreach (var op in OperationExtensions.Operations)
                        {
                            var attrib = op.GetAttributes();

                            if (attrib.ThreeOperand && lex.OperatorB == attrib.Operator)
                            {
                                return new Instruction(op, GetValue(lex.Destination, attrib.DestLabel), GetValue(lex.Source, attrib.SrcLabel), GetValue(lex.Target, attrib.TargLabel));
                            }
                        }
                    }
                }
                else if (lex.TwoOperand && !lex.Error)
                {
                    if (lex.OperatorA.EndsWith(Assignment) && lex.OperatorA.Length > 1)
                    {
                        var internalOp = lex.OperatorA.Substring(0, lex.OperatorA.Length - 1);

                        return ParseLexResult(new LexResult
                        {
                            OperatorA = Assignment,
                            OperatorB = internalOp,
                            Destination = lex.Destination,
                            Source = lex.Destination,
                            Target = lex.Source
                        });
                    }
                    else
                    {
                        foreach (var op in OperationExtensions.Operations)
                        {
                            var attrib = op.GetAttributes();

                            if (attrib.TwoOperand && lex.OperatorA == attrib.Operator)
                            {
                                return new Instruction(op, GetValue(lex.Destination, attrib.DestLabel), GetValue(lex.Source, attrib.SrcLabel));
                            }
                        }
                    }
                }
                else if (lex.OneOperand && !lex.Error)
                {
                    foreach (var op in OperationExtensions.Operations)
                    {
                        var attrib = op.GetAttributes();

                        if (attrib.OneOperand && lex.OperatorA == attrib.Operator)
                        {
                            return new Instruction(op, GetValue(lex.Destination, attrib.DestLabel));
                        }
                    }
                }
                else if (lex.Keyword && !lex.Error)
                {
                    foreach (var op in OperationExtensions.Operations)
                    {
                        var attrib = op.GetAttributes();

                        if (attrib.ZeroOperand && lex.OperatorA == attrib.Operator)
                        {
                            return new Instruction(op);
                        }
                    }
                }

                throw new CompileError(CompileError.InvalidSyntax);
            }

            return null;
        }

        public Operand GetValue(string str, bool wantsLabel)
        {
            var pending = GetImmediate(str);

            if (pending.HasValue)
            {
                if (wantsLabel) throw new CompileError(CompileError.ImmediateAsLabel);

                return pending.Value;
            }
            else
            {
                return wantsLabel ? GetLabel(str) : GetVariable(str);
            }
        }

        public Operand GetLabel(string name)
        {
            if (!Labels.TryGetValue(name, out ulong value))
            {
                if (Variables.ContainsKey(name)) throw new CompileError(CompileError.VariableAsLabel);

                value = NextLabel++;

                Labels.Add(name, value);
            }

            return new Operand(value, true, true);
        }

        public Operand? GetImmediate(string str)
        {
            if (!ulong.TryParse(str, out ulong result))
            {
                if (long.TryParse(str, out long signed))
                {
                    result = (ulong)signed;
                }
                else
                {
                    return null;
                }
            }

            return new Operand(result, true, false);
        }

        private Operand GetVariable(string name)
        {
            if (!Variables.TryGetValue(name, out ulong value))
            {
                if (Labels.ContainsKey(name)) throw new CompileError(CompileError.LabelAsVariable);

                value = NextVariable++;

                Variables.Add(name, value);
            }

            return new Operand(value, false, false);
        }

        public static LexResult Lex(string line)
        {
            line = line.Trim();

            if (line.Length == 0 || line.StartsWith(Comment)) return new LexResult();

            var match = ThreeOperand.Match(line);

            if (match.Success)
            {
                return new LexResult
                {
                    Destination = match.Groups[1].Value,
                    OperatorA = match.Groups[2].Value,
                    Source = match.Groups[3].Value,
                    OperatorB = match.Groups[4].Value,
                    Target = match.Groups[5].Value
                };
            }

            match = TwoOperand.Match(line);

            if (match.Success)
            {
                return new LexResult
                {
                    Destination = match.Groups[1].Value,
                    OperatorA = match.Groups[2].Value,
                    Source = match.Groups[3].Value
                };
            }

            match = OneOperand.Match(line);

            if (match.Success)
            {
                return new LexResult
                {
                    Destination = match.Groups[1].Value,
                    OperatorA = match.Groups[2].Value
                };
            }

            match = KeywordOperand.Match(line);

            if (match.Success)
            {
                return new LexResult
                {
                    Destination = match.Groups[2].Value,
                    OperatorA = match.Groups[1].Value
                };
            }

            match = Keyword.Match(line);

            if (match.Success)
            {
                return new LexResult
                {
                    OperatorA = match.Groups[1].Value
                };
            }

            return new LexResult 
            {
                Error = true
            };
        }
    }

    public struct LexResult
    {
        public bool Keyword => !(OperatorA is null) && Destination is null && Source is null && OperatorB is null && Target is null;
        public bool OneOperand => !(Destination is null || OperatorA is null) && Source is null && OperatorB is null && Target is null;
        public bool TwoOperand => !(Destination is null || OperatorA is null || Source is null) && OperatorB is null && Target is null;
        public bool ThreeOperand => !(Destination is null || OperatorA is null || Source is null || OperatorB is null || Target is null);

        public bool Valid => !(OperatorA is null && OperatorB is null && Destination is null && Source is null && Target is null);

        public string OperatorA;
        public string OperatorB;
        public string Destination;
        public string Source;
        public string Target;
        public bool Error;
    }
}
