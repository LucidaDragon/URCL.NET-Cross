using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace URCL.NET.Compiler
{
    public static class Parser
    {
        private static readonly string[] IgnoredOps = new[] { "BITS", "MINREG", "IMPORT" };

        public static IEnumerable<UrclInstruction> Parse(IEnumerable<string> lines)
        {
            var labels = new Dictionary<string, Label>();

            ulong ram = 1024;
            ulong maxReg = 0;
            for (int parseIteration = 0; parseIteration < 3; parseIteration++)
            {
                int index = 0;
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();

                    if (trimmed.Length != 0 && !line.StartsWith("//"))
                    {
                        UrclInstruction result = null;

                        try
                        {
                            result = ParseInstruction(trimmed, parseIteration == 0, parseIteration > 0, labels);

                            if (parseIteration == 1)
                            {
                                if (result.Operation == Operation.MINRAM && result.A > ram)
                                {
                                    ram = result.A;
                                }
                                else
                                {
                                    if (result.AType == OperandType.Register && result.A > maxReg) maxReg = result.A;
                                    if (result.BType == OperandType.Register && result.B > maxReg) maxReg = result.B;
                                    if (result.CType == OperandType.Register && result.C > maxReg) maxReg = result.C;
                                }

                                result = null;
                            }
                            else if (result != null && result.Operation == Operation.MINRAM)
                            {
                                result = null;
                            }
                        }
                        catch (ParserError ex)
                        {
                            throw new ParserError($"Error on line {index + 1}: \"{line}\" {ex.Message}");
                        }

                        if (result != null) yield return result;
                    }

                    index++;
                }

                if (parseIteration == 1)
                {
                    yield return new UrclInstruction(Operation.COMPILER_MAXREG, OperandType.Immediate, maxReg);
                    yield return new UrclInstruction(Operation.MINRAM, OperandType.Immediate, ram);
                }
            }
        }

        private static UrclInstruction ParseInstruction(string line, bool labelsOnly, bool instructionsOnly, Dictionary<string, Label> labels)
        {
            if (line.StartsWith('.'))
            {
                if (!instructionsOnly)
                {
                    var label = new Label();

                    if (labels.ContainsKey(line)) throw new ParserError("Label already defined.");

                    labels.Add(line, label);

                    return new UrclInstruction(Operation.COMPILER_CREATELABEL, label);
                }
                else if (labels.TryGetValue(line, out Label l))
                {
                    return new UrclInstruction(Operation.COMPILER_MARKLABEL, l);
                }
                else
                {
                    throw new ParserError("Label could not be registered.");
                }
            }
            else if (line.StartsWith('@') && !labelsOnly)
            {
                return new UrclInstruction(Operation.COMPILER_PRAGMA, line.Length > 1 ? line.Substring(1).Split(' ') : new string[0]);
            }
            else if (!labelsOnly)
            {
                var args = line.Replace(",", " ").Split(" ", StringSplitOptions.RemoveEmptyEntries);
                args[0] = args[0].ToUpper();

                if (Enum.TryParse(typeof(Operation), args[0], out object opValue))
                {
                    var op = (Operation)opValue;
                    var allowedTypes = GetOperationOperands(op);
                    var values = new object[args.Length - 1];
                    var valueTypes = new OperandType[values.Length];

                    if (values.Length != allowedTypes.Length) throw new ParserError($"Expected {allowedTypes.Length} operands but found {values.Length} instead.");

                    for (int i = 0; i < values.Length; i++)
                    {
                        var arg = args[i + 1].Trim();

                        if (arg.StartsWith("."))
                        {
                            if (labels.TryGetValue(arg, out Label label))
                            {
                                values[i] = label;
                                valueTypes[i] = OperandType.Label;
                            }
                            else
                            {
                                throw new ParserError($"Label \"{arg}\" is not defined.");
                            }
                        }
                        else if (arg.StartsWith("R") || arg.StartsWith('$'))
                        {
                            if (ulong.TryParse(arg[1..], out ulong v))
                            {
                                values[i] = v;
                                valueTypes[i] = OperandType.Register;
                            }
                            else
                            {
                                throw new ParserError($"Invalid register \"{arg}\".");
                            }
                        }
                        else if (arg.StartsWith("0x") && ulong.TryParse(arg[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong v))
                        {
                            values[i] = v;
                            valueTypes[i] = OperandType.Immediate;
                        }
                        else if (ulong.TryParse(arg, out v))
                        {
                            values[i] = v;
                            valueTypes[i] = OperandType.Immediate;
                        }
                        else
                        {
                            throw new ParserError($"Invalid operand \"{arg}\".");
                        }

                        if (!allowedTypes[i].HasFlag(valueTypes[i]))
                        {
                            throw new ParserError($"Operand {i + 1} of {op} does not accept {valueTypes[i].ToString().ToLower()}s.");
                        }
                    }

                    return new UrclInstruction(op, valueTypes, values);
                }
                else if (IgnoredOps.Contains(args[0]))
                {
                    return null;
                }
                else
                {
                    throw new ParserError($"Unknown instruction {args[0]}.");
                }
            }
            else
            {
                return null;
            }
        }

        private static OperandType[] GetOperationOperands(Operation operation)
        {
            var attrib = typeof(Operation)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.Name == operation.ToString())
                .FirstOrDefault()
                .GetCustomAttribute<AcceptsAttribute>();

            if (attrib is null) return new OperandType[0];

            return attrib.Types;
        }
    }
}
