using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SpeedAsm.Extensions
{
    public class FunctionProcessor : ILinePreparser, IPostProcess
    {
        private const string ExpectedBegin = "Expected beginning of function body.";
        private const string ExpectedEnd = "Expected end of function body.";

        private const string FunctionDefRegex = @"func\s+([\w\d_]+)\(\s*(([\w\d_]+\s*,\s)*[\w\d_]+\s*)\)\s*({)?";

        private FunctionDefinition Definition;
        private bool ExpectingBlock;
        private int Depth;
        private List<Instruction> Body;

        public void Begin(Parser parser)
        {
            Definition = default;
            ExpectingBlock = false;
            Depth = 0;
            Body = new List<Instruction>();
        }

        public void End(Parser parser)
        {
            if (ExpectingBlock)
            {
                throw new CompileError(ExpectedEnd);
            }
        }

        public void Generated(Parser parser, Instruction inst) { }

        public IEnumerable<Instruction> Process(Parser parser, Instruction inst)
        {
            if (inst.Operation == Operation.CompilerGenerated)
            {
                if (inst.Data is FunctionDefinition def)
                {
                    Definition = def;
                    ExpectingBlock = true;

                    yield return new Instruction(Operation.Label, parser.GetLabel(Definition.Name));
                }
                else if (inst.Data is BlockBegin && (ExpectingBlock || Depth > 0))
                {
                    ExpectingBlock = false;
                    Depth++;
                }
                else if (inst.Data is BlockEnd && Depth > 0)
                {
                    Depth--;

                    if (Depth == 0)
                    {
                        foreach (var childInst in Body)
                        {
                            yield return childInst;
                        }

                        Body.Clear();
                    }
                }
                else if (ExpectingBlock)
                {
                    throw new CompileError(ExpectedBegin);
                }
            }
            else if (ExpectingBlock)
            {
                throw new CompileError(ExpectedBegin);
            }

            if (Depth > 0)
            {
                Body.Add(inst);
            }
            else
            {
                yield return inst;
            }
        }

        public bool TryParse(Parser parser, string line, out IEnumerable<Instruction> result)
        {
            line = line.Trim();

            var match = Regex.Match(line, FunctionDefRegex);

            var buffer = new List<Instruction>();

            if (match.Success)
            {
                var funcName = match.Groups[1].Value;
                var args = match.Groups[2].Value;
                var begin = match.Groups[4].Success;

                buffer.Add(new Instruction(new FunctionDefinition(funcName, args.Split(',').Select(s => s.Trim()).ToArray())));
                if (begin) buffer.Add(new Instruction(new BlockBegin()));
            }
            else if (line == "{")
            {
                buffer.Add(new Instruction(new BlockBegin()));
            }
            else if (line == "}")
            {
                buffer.Add(new Instruction(new BlockEnd()));
            }

            result = buffer;

            return buffer.Count > 0;
        }

        private struct FunctionDefinition
        {
            public string Name;
            public string[] Parameters;

            public FunctionDefinition(string name, string[] parameters)
            {
                Name = name;
                Parameters = parameters;
            }
        }
    }

    public struct BlockBegin { }
    public struct BlockEnd { }

    public static class FunctionProcessorEx
    {
        public static Parser AddFunctionProcessor(this Parser parser)
        {
            var processor = new FunctionProcessor();
            parser.LinePreparsers.Add(processor);
            parser.PostProcesses.Add(processor);
            return parser;
        }
    }
}
