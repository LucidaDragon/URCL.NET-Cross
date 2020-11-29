using System;
using System.Collections.Generic;
using System.Reflection;

namespace URCL.NET
{
    public class UrclOptimizer
    {
        public bool All
        {
            set
            {
                CullRedundantStackOps = value;
                CullCreateLabel = value;
                CullComments = value;
                CullPragmas = value;
                ReplaceImmZeroWithZeroRegister = value;
            }
        }

        public bool CullRedundantStackOps { get; set; } = false;
        public bool CullCreateLabel { get; set; } = false;
        public bool CullComments { get; set; } = false;
        public bool CullPragmas { get; set; } = false;

        public OperationType Compatibility { get; set; } = OperationType.CustomPragma;

        public bool ReplaceImmZeroWithZeroRegister { get; set; } = false;

        public UrclInstruction[] Optimize(UrclInstruction[] instructions)
        {
            var insts = new List<UrclInstruction>(instructions);

            for (int i = 0; i < insts.Count; i++)
            {
                var current = insts[i];

                UrclInstruction next = null;
                int gap = 1;
                for (int j = 1; i + j < insts.Count; j++, gap++)
                {
                    next = insts[i + j];

                    if (next.Operation != Operation.COMPILER_COMMENT) break;
                }

                if (CullRedundantStackOps && current.Operation == Operation.PSH)
                {
                    if (next != null && next.Operation == Operation.POP)
                    {
                        if (current.A == next.A)
                        {
                            RemoveInstructionRange(insts, i, gap + 1, CullComments);
                        }
                        else
                        {
                            RemoveInstructionRange(insts, i + 1, gap, CullComments);

                            current.Operation = Operation.MOV;

                            current.BType = current.AType;
                            current.B = current.A;
                            current.BLabel = current.ALabel;

                            current.AType = next.AType;
                            current.A = next.A;
                            current.ALabel = next.ALabel;
                        }

                        i = 0;
                    }
                }
                else if (ReplaceImmZeroWithZeroRegister && current.Operation == Operation.IMM)
                {
                    if (current.BType == OperandType.Immediate && current.B == 0)
                    {
                        insts[i] = new UrclInstruction(Operation.MOV, insts[i].AType, insts[i].A, OperandType.Register, 0);
                    }
                }
                else if (CullCreateLabel && current.Operation == Operation.COMPILER_CREATELABEL)
                {
                    insts.RemoveAt(i);
                    i = 0;
                }
                else if (CullComments && current.Operation == Operation.COMPILER_COMMENT)
                {
                    insts.RemoveAt(i);
                    i = 0;
                }
                else if (CullPragmas && current.Operation == Operation.COMPILER_PRAGMA)
                {
                    insts.RemoveAt(i);
                    i = 0;
                }
            }

            ulong temporaryRegister = 1;

            for (int i = 0; i < insts.Count; i++)
            {
                var inst = insts[i];

                if (inst.AType == OperandType.Register && inst.A == temporaryRegister) temporaryRegister++;
                if (inst.BType == OperandType.Register && inst.B == temporaryRegister) temporaryRegister++;
                if (inst.CType == OperandType.Register && inst.C == temporaryRegister) temporaryRegister++;
            }

            for (int i = 0; i < insts.Count; i++)
            {

            }

            return insts.ToArray();
        }

        private void RemoveInstructionRange(List<UrclInstruction> instructions, int start, int length, bool cullComments)
        {
            for (int i = 0; i < length; i++)
            {
                if (instructions[start].Operation != Operation.COMPILER_COMMENT || cullComments)
                {
                    instructions.RemoveAt(start);
                }
                else
                {
                    start++;
                }
            }
        }

        public static class Conversion
        {
            private static readonly Dictionary<Operation, MethodInfo> Converters = new Dictionary<Operation, MethodInfo>();

            static Conversion()
            {
                foreach (var method in typeof(Conversion).GetMethods())
                {
                    var attribs = method.GetCustomAttributes(typeof(TargetsAttribute), false);

                    if (method.IsStatic && attribs.Length > 0)
                    {
                        var parameters = method.GetParameters();

                        if (parameters.Length == 2 &&
                            parameters[0].ParameterType == typeof(UrclInstruction) &&
                            parameters[1].ParameterType == typeof(ulong) &&
                            method.ReturnType == typeof(IEnumerable<UrclInstruction>))
                        {
                            foreach (TargetsAttribute attrib in attribs)
                            {
                                if (Converters.ContainsKey(attrib.Target))
                                {
                                    throw new InvalidOperationException($"Multiple converters for instruction {attrib.Target}.");
                                }
                                else
                                {
                                    Converters.Add(attrib.Target, method);
                                }
                            }
                        }
                        else
                        {
                            throw new InvalidCastException($"\"{method.Name}\" is not a valid instruction converter.");
                        }
                    }
                }

                foreach (var field in typeof(Operation).GetFields())
                {
                    var attribs = field.GetCustomAttributes(typeof(AcceptsAttribute), false);

                    if (field.IsStatic && attribs.Length == 1 && field.FieldType == typeof(Operation))
                    {
                        var attrib = (AcceptsAttribute)attribs[0];
                        var value = (Operation)field.GetValue(null);

                        if ((attrib.Type == OperationType.Complex || attrib.Type == OperationType.Custom) && !Converters.ContainsKey(value))
                        {
                            throw new InvalidCastException($"Required conversion for {value} is missing.");
                        }
                    }
                }
            }

            public static IEnumerable<UrclInstruction> Convert(UrclInstruction instruction, OperationType maximumTier, ulong temporaryRegister, out bool changed)
            {
                var attrib = instruction.Operation.GetAttributes();

                if (attrib.Type <= maximumTier)
                {
                    changed = false;
                    return new[] { instruction };
                }
                else
                {
                    if (Converters.TryGetValue(instruction.Operation, out MethodInfo converter))
                    {
                        changed = true;
                        return (IEnumerable<UrclInstruction>)converter.Invoke(null, new object[] { instruction, temporaryRegister });
                    }
                    else
                    {
                        if (attrib.Type == OperationType.CustomPragma)
                        {
                            changed = true;
                            return new UrclInstruction[0];
                        }
                        else
                        {
                            changed = true;
                            return new[]
                            {
                                new UrclInstruction(Operation.COMPILER_PRAGMA, new[]
                                {
                                    "WARNING",
                                    "CONVERSION",
                                    instruction.Operation.ToString().ToUpper(),
                                    maximumTier.ToString().ToUpper()
                                }),
                                new UrclInstruction(Operation.COMPILER_COMMENT, new[]
                                {
                                    $"Failed to convert operation {instruction.Operation} to {maximumTier.ToString().ToLower()}"
                                })
                            };
                        }
                    }
                }
            }

#pragma warning disable IDE0060 // Remove unused parameter
            [Targets(Operation.BRK)]
            public static IEnumerable<UrclInstruction> BRK(UrclInstruction inst, ulong temporary)
            {
                return new[] { new UrclInstruction(Operation.HLT) };
            }

            [Targets(Operation.MLT)]
            public static IEnumerable<UrclInstruction> MLT(UrclInstruction inst, ulong temporary)
            {
                var result = new List<UrclInstruction>();
                var loop = new Label();
                var skip = new Label();

                SaveCheck(inst, out bool saveA, out bool saveB, out bool saveC, out _);

                if (saveA) result.Add(new UrclInstruction(Operation.PSH, OperandType.Register, 1));
                if (saveB) result.Add(new UrclInstruction(Operation.PSH, OperandType.Register, 2));
                if (saveC) result.Add(new UrclInstruction(Operation.PSH, OperandType.Register, 3));

                OrganizeInputs(result.Add, inst, temporary);

                result.Add(new UrclInstruction(Operation.MOV, OperandType.Register, 3, OperandType.Register, 0));

                result.Add(new UrclInstruction(Operation.COMPILER_MARKLABEL, loop));

                result.Add(new UrclInstruction(Operation.AND, OperandType.Register, 0, OperandType.Register, 1, OperandType.Immediate, 1));

                result.Add(new UrclInstruction(Operation.BRZ, skip));

                result.Add(new UrclInstruction(Operation.ADD, OperandType.Register, 3, OperandType.Register, 3, OperandType.Register, 2));

                result.Add(new UrclInstruction(Operation.COMPILER_MARKLABEL, skip));

                result.Add(new UrclInstruction(Operation.LSH, OperandType.Register, 2, OperandType.Register, 2));
                result.Add(new UrclInstruction(Operation.RSH, OperandType.Register, 3, OperandType.Register, 3));

                result.Add(new UrclInstruction(Operation.BNZ, loop));

                if (inst.A != 3) result.Add(new UrclInstruction(Operation.MOV, OperandType.Register, inst.A, OperandType.Register, 3));

                if (saveC) result.Add(new UrclInstruction(Operation.POP, OperandType.Register, 3));
                if (saveB) result.Add(new UrclInstruction(Operation.POP, OperandType.Register, 2));
                if (saveA) result.Add(new UrclInstruction(Operation.POP, OperandType.Register, 1));

                return result;
            }

            [Targets(Operation.DIV)]
            public static IEnumerable<UrclInstruction> DIV(UrclInstruction inst, ulong temporary)
            {
                var result = new List<UrclInstruction>();
                var top = new Label();
                var loop = new Label();
                var skipA = new Label();
                var skipB = new Label();
                var isZero = new Label();
                var end = new Label();

                SaveCheck(inst, out bool saveA, out bool saveB, out bool saveC, out bool saveD);

                if (saveA) result.Add(new UrclInstruction(Operation.PSH, OperandType.Register, 1));
                if (saveB) result.Add(new UrclInstruction(Operation.PSH, OperandType.Register, 2));
                if (saveC) result.Add(new UrclInstruction(Operation.PSH, OperandType.Register, 3));
                if (saveD) result.Add(new UrclInstruction(Operation.PSH, OperandType.Register, 4));

                OrganizeInputs(result.Add, inst, temporary);

                result.Add(new UrclInstruction(Operation.OR, OperandType.Register, 0, OperandType.Register, 2, OperandType.Register, 2));
                
                result.Add(new UrclInstruction(Operation.BRZ, isZero));
                
                result.Add(new UrclInstruction(Operation.MOV, OperandType.Register, 3, OperandType.Register, 0));
                result.Add(new UrclInstruction(Operation.IMM, OperandType.Register, 4, OperandType.Immediate, 1));
                
                result.Add(new UrclInstruction(Operation.COMPILER_MARKLABEL, top));
                
                result.Add(new UrclInstruction(Operation.LSH, OperandType.Register, 3, OperandType.Register, 3));
                result.Add(new UrclInstruction(Operation.LSH, OperandType.Register, 1, OperandType.Register, 1));
                
                result.Add(new UrclInstruction(Operation.BRC, skipA));
                
                result.Add(new UrclInstruction(Operation.COMPILER_MARKLABEL, loop));
                
                result.Add(new UrclInstruction(Operation.SUB, OperandType.Register, 0, OperandType.Register, 3, OperandType.Register, 2));

                result.Add(new UrclInstruction(Operation.BRN, skipB));
                
                result.Add(new UrclInstruction(Operation.SUB, OperandType.Register, 3, OperandType.Register, 3, OperandType.Register, 2));
                result.Add(new UrclInstruction(Operation.INC, OperandType.Register, 1, OperandType.Register, 1));
                
                result.Add(new UrclInstruction(Operation.COMPILER_MARKLABEL, skipB));

                result.Add(new UrclInstruction(Operation.LSH, OperandType.Register, 4, OperandType.Register, 4));

                result.Add(new UrclInstruction(Operation.BRC, end));
                
                result.Add(new UrclInstruction(Operation.BRA, top));
                
                result.Add(new UrclInstruction(Operation.COMPILER_MARKLABEL, skipA));
                
                result.Add(new UrclInstruction(Operation.INC, OperandType.Register, 3, OperandType.Register, 3));
                
                result.Add(new UrclInstruction(Operation.BRA, loop));
                
                result.Add(new UrclInstruction(Operation.COMPILER_MARKLABEL, isZero));
                
                result.Add(new UrclInstruction(Operation.BRK));
                
                result.Add(new UrclInstruction(Operation.COMPILER_MARKLABEL, end));

                if (inst.A != 1) result.Add(new UrclInstruction(Operation.MOV, OperandType.Register, inst.A, OperandType.Register, 1));

                if (saveD) result.Add(new UrclInstruction(Operation.POP, OperandType.Register, 4));
                if (saveC) result.Add(new UrclInstruction(Operation.POP, OperandType.Register, 3));
                if (saveB) result.Add(new UrclInstruction(Operation.POP, OperandType.Register, 2));
                if (saveA) result.Add(new UrclInstruction(Operation.POP, OperandType.Register, 1));

                return result;
            }

            [Targets(Operation.MOD)]
            public static IEnumerable<UrclInstruction> MOD(UrclInstruction inst, ulong temporary)
            {
                var result = new List<UrclInstruction>();
                var loop = new Label();

                SaveCheck(inst, out bool saveA, out bool saveB, out _, out _);

                if (saveA) result.Add(new UrclInstruction(Operation.PSH, OperandType.Register, 1));
                if (saveB) result.Add(new UrclInstruction(Operation.PSH, OperandType.Register, 2));

                OrganizeInputs(result.Add, inst, temporary);

                result.Add(new UrclInstruction(Operation.OR, OperandType.Register, 0, OperandType.Register, 2, OperandType.Register, 2));

                result.Add(new UrclInstruction(Operation.BNZ, loop));

                result.Add(new UrclInstruction(Operation.BRK));

                result.Add(new UrclInstruction(Operation.COMPILER_MARKLABEL, loop));

                result.Add(new UrclInstruction(Operation.SUB, OperandType.Register, 1, OperandType.Register, 1, OperandType.Register, 2));

                result.Add(new UrclInstruction(Operation.BNC, loop));

                result.Add(new UrclInstruction(Operation.ADD, OperandType.Register, 1, OperandType.Register, 1, OperandType.Register, 2));

                if (inst.A != 1) result.Add(new UrclInstruction(Operation.MOV, OperandType.Register, inst.A, OperandType.Register, 1));

                if (saveB) result.Add(new UrclInstruction(Operation.POP, OperandType.Register, 2));
                if (saveA) result.Add(new UrclInstruction(Operation.POP, OperandType.Register, 1));

                return result;
            }

            [Targets(Operation.BSL)]
            public static IEnumerable<UrclInstruction> BSL(UrclInstruction inst, ulong temporary)
            {
                var result = new List<UrclInstruction>();
                var loop = new Label();
                var end = new Label();

                SaveCheck(inst, out bool saveA, out bool saveB, out _, out _);

                result.Add(new UrclInstruction(Operation.OR, OperandType.Register, 0, OperandType.Register, inst.C, OperandType.Register, inst.C));

                result.Add(new UrclInstruction(Operation.BRZ, end));

                if (saveA) result.Add(new UrclInstruction(Operation.PSH, OperandType.Register, 1));
                if (saveB) result.Add(new UrclInstruction(Operation.PSH, OperandType.Register, 2));

                OrganizeInputs(result.Add, inst, temporary);

                result.Add(new UrclInstruction(Operation.COMPILER_MARKLABEL, loop));

                result.Add(new UrclInstruction(Operation.LSH, OperandType.Register, 1, OperandType.Register, 1));
                result.Add(new UrclInstruction(Operation.DEC, OperandType.Register, 2, OperandType.Register, 2));

                result.Add(new UrclInstruction(Operation.BNZ, loop));

                if (inst.A != 1) result.Add(new UrclInstruction(Operation.MOV, OperandType.Register, inst.A, OperandType.Register, 1));

                if (saveB) result.Add(new UrclInstruction(Operation.POP, OperandType.Register, 2));
                if (saveA) result.Add(new UrclInstruction(Operation.POP, OperandType.Register, 1));

                result.Add(new UrclInstruction(Operation.COMPILER_MARKLABEL, end));

                return result;
            }

            [Targets(Operation.BSR)]
            public static IEnumerable<UrclInstruction> BSR(UrclInstruction inst, ulong temporary)
            {
                var result = new List<UrclInstruction>();
                var loop = new Label();
                var end = new Label();

                SaveCheck(inst, out bool saveA, out bool saveB, out _, out _);

                result.Add(new UrclInstruction(Operation.OR, OperandType.Register, 0, OperandType.Register, inst.C, OperandType.Register, inst.C));

                result.Add(new UrclInstruction(Operation.BRZ, end));

                if (saveA) result.Add(new UrclInstruction(Operation.PSH, OperandType.Register, 1));
                if (saveB) result.Add(new UrclInstruction(Operation.PSH, OperandType.Register, 2));

                OrganizeInputs(result.Add, inst, temporary);

                result.Add(new UrclInstruction(Operation.COMPILER_MARKLABEL, loop));

                result.Add(new UrclInstruction(Operation.RSH, OperandType.Register, 1, OperandType.Register, 1));
                result.Add(new UrclInstruction(Operation.DEC, OperandType.Register, 2, OperandType.Register, 2));

                result.Add(new UrclInstruction(Operation.BNZ, loop));

                if (inst.A != 1) result.Add(new UrclInstruction(Operation.MOV, OperandType.Register, inst.A, OperandType.Register, 1));

                if (saveB) result.Add(new UrclInstruction(Operation.POP, OperandType.Register, 2));
                if (saveA) result.Add(new UrclInstruction(Operation.POP, OperandType.Register, 1));

                result.Add(new UrclInstruction(Operation.COMPILER_MARKLABEL, end));

                return result;
            }

            [Targets(Operation.CAL)]
            public static IEnumerable<UrclInstruction> CAL(UrclInstruction inst, ulong temporary)
            {
                var result = new List<UrclInstruction>();
                var returnPoint = new Label();

                result.Add(new UrclInstruction(Operation.PSH, returnPoint));

                if (inst.AType == OperandType.Label)
                {
                    result.Add(new UrclInstruction(Operation.BRA, inst.ALabel));
                }
                else
                {
                    result.Add(new UrclInstruction(Operation.BRA, inst.AType, inst.A));
                }

                result.Add(new UrclInstruction(Operation.COMPILER_MARKLABEL, returnPoint));

                return result;
            }

            [Targets(Operation.RET)]
            public static IEnumerable<UrclInstruction> RET(UrclInstruction inst, ulong temporary)
            {
                return new[]
                {
                    new UrclInstruction(Operation.POP, OperandType.Register, temporary),
                    new UrclInstruction(Operation.BRA, OperandType.Register, temporary)
                };
            }
#pragma warning restore IDE0060 // Remove unused parameter

            private static void OrganizeInputs(Action<UrclInstruction> emit, UrclInstruction inst, ulong temporary)
            {
                if (inst.BType == OperandType.Register && inst.CType == OperandType.Register)
                {
                    if (inst.B == 2 && inst.C == 1)
                    {
                        emit(new UrclInstruction(Operation.MOV, OperandType.Register, temporary, OperandType.Register, inst.C));
                        emit(new UrclInstruction(Operation.MOV, OperandType.Register, 1, OperandType.Register, inst.B));
                        emit(new UrclInstruction(Operation.MOV, OperandType.Register, 2, OperandType.Register, temporary));
                    }
                    else if (inst.C == 1)
                    {
                        emit(new UrclInstruction(Operation.MOV, OperandType.Register, 2, OperandType.Register, inst.C));
                        emit(new UrclInstruction(Operation.MOV, OperandType.Register, 1, OperandType.Register, inst.B));
                    }
                    else
                    {
                        emit(new UrclInstruction(Operation.MOV, OperandType.Register, 1, OperandType.Register, inst.B));
                        emit(new UrclInstruction(Operation.MOV, OperandType.Register, 2, OperandType.Register, inst.C));
                    }
                }
                else
                {
                    GetOperand(emit, inst, 1, 1);
                    GetOperand(emit, inst, 2, 2);
                }
            }

            private static void GetOperand(Action<UrclInstruction> emit, UrclInstruction inst, ulong register, int operand)
            {
                switch (operand)
                {
                    case 0:
                        {
                            if (inst.AType == OperandType.Register && inst.A != register)
                            {
                                emit(new UrclInstruction(Operation.MOV, OperandType.Register, register, OperandType.Immediate, inst.A));
                            }
                            else if (inst.AType == OperandType.Immediate)
                            {
                                emit(new UrclInstruction(Operation.IMM, OperandType.Register, register, OperandType.Immediate, inst.A));
                            }
                            else if (inst.AType == OperandType.Label)
                            {
                                emit(new UrclInstruction(Operation.IMM, OperandType.Register, register, inst.ALabel));
                            }
                        }
                        break;
                    case 1:
                        {
                            if (inst.BType == OperandType.Register && inst.B != register)
                            {
                                emit(new UrclInstruction(Operation.MOV, OperandType.Register, register, OperandType.Immediate, inst.B));
                            }
                            else if (inst.BType == OperandType.Immediate)
                            {
                                emit(new UrclInstruction(Operation.IMM, OperandType.Register, register, OperandType.Immediate, inst.B));
                            }
                            else if (inst.BType == OperandType.Label)
                            {
                                emit(new UrclInstruction(Operation.IMM, OperandType.Register, register, inst.BLabel));
                            }
                        }
                        break;
                    case 2:
                        {
                            if (inst.CType == OperandType.Register && inst.C != register)
                            {
                                emit(new UrclInstruction(Operation.MOV, OperandType.Register, register, OperandType.Immediate, inst.C));
                            }
                            else if (inst.CType == OperandType.Immediate)
                            {
                                emit(new UrclInstruction(Operation.IMM, OperandType.Register, register, OperandType.Immediate, inst.C));
                            }
                            else if (inst.CType == OperandType.Label)
                            {
                                emit(new UrclInstruction(Operation.IMM, OperandType.Register, register, inst.CLabel));
                            }
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private static void SaveCheck(UrclInstruction inst, out bool saveA, out bool saveB, out bool saveC, out bool saveD)
            {
                saveA = false;
                saveB = false;
                saveC = false;
                saveD = false;

                if (inst.AType == OperandType.Register)
                {
                    if (inst.A != 1)
                    {
                        saveA = true;
                    }

                    if (inst.A != 2)
                    {
                        saveB = true;
                    }

                    if (inst.A != 3)
                    {
                        saveC = true;
                    }

                    if (inst.A != 4)
                    {
                        saveD = true;
                    }
                }
            }

            [AttributeUsage(AttributeTargets.Method)]
            public class TargetsAttribute : Attribute
            {
                public Operation Target;

                public TargetsAttribute(Operation target)
                {
                    Target = target;
                }
            }
        }
    }
}
