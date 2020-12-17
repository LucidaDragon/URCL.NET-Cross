using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace URCL.NET.Compiler
{
    public class Parser
    {
        private static readonly string[] IgnoredOps = new[] { "MINREG" };

        private readonly Dictionary<string, Label> Labels = new Dictionary<string, Label>();
        private readonly Dictionary<string, ulong> ValueMacros = new Dictionary<string, ulong>();
        private readonly Dictionary<string, IEnumerable<UrclInstruction>> CodeMacros = new Dictionary<string, IEnumerable<UrclInstruction>>();

        public IEnumerable<UrclInstruction> Parse(IEnumerable<string> lines, Func<string, IEnumerable<string>> import, string sourceName = null)
        {
            var lineLabels = new Label[lines.Count()];
            var used = new bool[lineLabels.Length];

            for (int parseIteration = 0; parseIteration < 3; parseIteration++)
            {
                string macroName = null;
                var macroBuffer = new List<UrclInstruction>();

                int index = 0;
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();

                    if (!ParserIgnore(trimmed))
                    {
                        if (parseIteration == 0) lineLabels[index] = new Label();

                        UrclInstruction result = null;
                        IEnumerable<UrclInstruction> block = null;

                        try
                        {
                            result = ParseInstruction(trimmed, parseIteration == 0, parseIteration > 0, Labels, ValueMacros, CodeMacros, (rel) => 
                            {
                                var target = rel + index;

                                if (target < 0 || target >= lineLabels.Length || lineLabels[target] is null)
                                {
                                    throw new ParserError("Relative address is out of bounds.");
                                }

                                used[target] = true;
                                return lineLabels[target];
                            });

                            if (parseIteration == 1 && result != null)
                            {
                                if (result.Operation == Operation.COMPILER_CODEMACRO_BEGIN)
                                {
                                    if (macroName != null) throw new ParserError("Macro was not finished before starting another macro.");
                                    macroName = result.Arguments[0];
                                    macroBuffer.Clear();
                                    result = null;
                                }
                                else if (result.Operation == Operation.COMPILER_CODEMACRO_END)
                                {
                                    if (macroName == null) throw new ParserError("Missing beginning of macro.");
                                    CodeMacros[macroName] = macroBuffer.ToArray();
                                    macroName = null;
                                    macroBuffer.Clear();
                                    result = null;
                                }
                                else if (result.Operation == Operation.COMPILER_CODEMACRO_USE)
                                {
                                    if (macroName != null) throw new ParserError("Nested macros are not supported.");
                                    if (CodeMacros.TryGetValue(result.Arguments[0], out IEnumerable<UrclInstruction> insts))
                                    {
                                        block = insts;
                                        result = null;
                                    }
                                    else
                                    {
                                        throw new ParserError($"Undefined macro \"{result.Arguments[0]}\"");
                                    }
                                }
                                else if (result.Operation == Operation.IMPORT)
                                {
                                    var name = result.Arguments[0];
                                    block = Parse(import(string.Join(' ', result.Arguments)), import, sourceName).ToArray();
                                    result = null;
                                }
                                else if (macroName != null)
                                {
                                    macroBuffer.Add(result);
                                    result = null;
                                }

                                result = null;
                            }
                            else if (result != null)
                            {
                                if (result.Operation == Operation.MINRAM || result.Operation == Operation.IMPORT)
                                {
                                    result = null;
                                }
                            }
                        }
                        catch (TargetInvocationException ex)
                        {
                            throw new ParserError($"Error on line {index + 1}: \"{line}\" {ex.InnerException.Message}");
                        }
                        catch (ParserError ex)
                        {
                            throw new ParserError($"Error on line {index + 1}: \"{line}\" {ex.Message}");
                        }

                        if ((result != null || block != null) && used[index])
                        {
                            yield return new UrclInstruction(Operation.COMPILER_MARKLABEL, lineLabels[Array.IndexOf(lineLabels, lineLabels[index])]);
                        }

                        if (result != null) yield return result;
                        if (block != null)
                        {
                            foreach (var inst in block)
                            {
                                yield return inst;
                            }
                        }
                    }
                    else
                    {
                        if (parseIteration == 0)
                        {
                            if (index > 0)
                            {
                                lineLabels[index] = lineLabels[index - 1];
                            }
                            else
                            {
                                lineLabels[index] = null;
                            }
                        }
                    }

                    index++;
                }
            }
        }

        private static UrclInstruction ParseInstruction<TMacro>(string line, bool labelsOnly, bool instructionsOnly, Dictionary<string, Label> labels, Dictionary<string, ulong> valueMacros, Dictionary<string, TMacro> codeMacros, Func<long, Label> getRelative)
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
                var arguments = line.Length > 1 ? line.Substring(1).Split(' ') : new string[0];
                string type = string.Empty;

                if (arguments.Length > 0) type = arguments[0];

                if (type.ToLower() == "macro")
                {
                    if (arguments.Length == 3)
                    {
                        var name = arguments[1];
                        var value = arguments[2];

                        if (name.ToLower() == "begin")
                        {
                            return new UrclInstruction(Operation.COMPILER_CODEMACRO_BEGIN, new[] { value });
                        }
                        else if (ulong.TryParse(value, out ulong v) ||
                            (value.StartsWith("0x") && ulong.TryParse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v)) ||
                            (value.StartsWith("0b") && TryParseBinary(value.Substring(2), out v)))
                        {
                            valueMacros[name] = v;
                            return null;
                        }
                    }
                    else if (arguments.Length == 2)
                    {
                        var tag = arguments[1];

                        if (tag.ToLower() == "end")
                        {
                            return new UrclInstruction(Operation.COMPILER_CODEMACRO_END);
                        }
                        else
                        {
                            return new UrclInstruction(Operation.COMPILER_CODEMACRO_USE, new[] { tag });
                        }
                    }

                    throw new ParserError("Invalid macro.");
                }
                else
                {
                    return new UrclInstruction(Operation.COMPILER_PRAGMA, arguments);
                }
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
                    ulong v;

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
                            if (ulong.TryParse(arg[1..], out v))
                            {
                                values[i] = v;
                                valueTypes[i] = OperandType.Register;
                            }
                            else
                            {
                                throw new ParserError($"Invalid register \"{arg}\".");
                            }
                        }
                        else if (arg.StartsWith('+') && long.TryParse(arg.Substring(1), out long rel))
                        {
                            values[i] = getRelative(rel);
                            valueTypes[i] = OperandType.Label;
                        }
                        else if (arg.StartsWith('-') && long.TryParse(arg, out rel))
                        {
                            values[i] = getRelative(rel);
                            valueTypes[i] = OperandType.Label;
                        }
                        else if (TryParseValue(arg, out v))
                        {
                            values[i] = v;
                            valueTypes[i] = OperandType.Immediate;
                        }
                        else if (valueMacros.TryGetValue(arg, out v))
                        {
                            values[i] = v;
                            valueTypes[i] = OperandType.Immediate;
                        }
                        else if (allowedTypes.Length == 1 && allowedTypes[0] == OperandType.String)
                        {
                            values[i] = args.Skip(1).Select(str => str.Trim()).ToArray();
                            valueTypes[i] = OperandType.String;
                            if (allowedTypes[i].HasFlag(valueTypes[i])) break;
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

        public static bool TryParseValue(string str, out ulong result)
        {
            if (str.StartsWith("0x") && ulong.TryParse(str[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result))
            {
                return true;
            }
            else if (str.StartsWith("0b") && TryParseBinary(str[2..], out result))
            {
                return true;
            }
            else if (ulong.TryParse(str, out result))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool TryParseBinary(string str, out ulong result)
        {
            result = 0;

            if (str.Length > 64) return false;

            for (int i = 0; i < str.Length; i++)
            {
                result <<= 1;

                if (str[i] == '1')
                {
                    result |= 1;
                }
                else if (str[i] != '0')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ParserIgnore(string line)
        {
            line = line.Trim();
            return line.Length == 0 || line.StartsWith("//");
        }
    }
}
