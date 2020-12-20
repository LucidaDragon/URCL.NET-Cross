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

        private readonly Dictionary<string, ulong> Labels = new Dictionary<string, ulong>();
        private readonly Dictionary<string, ulong> Variables = new Dictionary<string, ulong>();
        private ulong NextLabel = 0;
        private ulong NextVariable = 0;

        public IEnumerable<Instruction> Parse(IEnumerable<string> lines)
        {
            foreach (var parser in LinePreparsers)
            {
                parser.Begin();
            }

            var lineIndex = 0;

            foreach (var line in lines)
            {
                var generated = Parse(line);

                foreach (var inst in generated)
                {
                    inst.Line = lineIndex;

                    foreach (var parser in LinePreparsers)
                    {
                        parser.Generated(inst);
                    }

                    yield return inst;
                }

                lineIndex++;
            }

            foreach (var parser in LinePreparsers)
            {
                parser.End();
            }
        }

        private IEnumerable<Instruction> Parse(string line)
        {
            foreach (var parser in LinePreparsers)
            {
                if (parser.TryParse(line, out IEnumerable<Instruction> result))
                {
                    return result;
                }
            }

            var inst = Parse(Lex(line));

            return inst != null ? new[] { inst } : new Instruction[0];
        }

        private Instruction Parse(LexResult lex)
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

                        return Parse(new LexResult
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

        private Operand GetValue(string str, bool wantsLabel)
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

        private Operand GetLabel(string name)
        {
            if (!Labels.TryGetValue(name, out ulong value))
            {
                if (Variables.ContainsKey(name)) throw new CompileError(CompileError.VariableAsLabel);

                value = NextLabel++;

                Labels.Add(name, value);
            }

            return new Operand(value, true, true);
        }

        private Operand? GetImmediate(string str)
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

        private static LexResult Lex(string line)
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
