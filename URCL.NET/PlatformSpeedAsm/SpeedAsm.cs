using SpeedAsm;
using System;
using System.Collections.Generic;

namespace URCL.NET.PlatformSpeedAsm
{
    public class SpeedAsm : IEmitter
    {
        public readonly Queue<UrclInstruction> Instructions = new Queue<UrclInstruction>();

        private readonly List<UrclInstruction> Urcl = new List<UrclInstruction>();
        private readonly Dictionary<ulong, Label> Labels = new Dictionary<ulong, Label>();

        public void Emit(Instruction inst)
        {
            var start = Urcl.Count;

            var result = new UrclInstruction(Operation.NOP)
            {
                A = inst.Destination.Value,
                B = inst.Source.Value,
                C = inst.Target.Value,

                AType = inst.Destination.Immediate ? OperandType.Immediate : OperandType.Register,
                BType = inst.Source.Immediate ? OperandType.Immediate : OperandType.Register,
                CType = inst.Target.Immediate ? OperandType.Immediate : OperandType.Register
            };

            if (result.AType == OperandType.Register) result.A++;
            if (result.BType == OperandType.Register) result.B++;
            if (result.CType == OperandType.Register) result.C++;

            switch (inst.Operation)
            {
                case global::SpeedAsm.Operation.Set:
                    result.Operation = inst.Source.Immediate ? Operation.IMM : Operation.MOV;
                    result.CType = OperandType.None;
                    break;
                case global::SpeedAsm.Operation.Add:
                    result.Operation = Operation.ADD;
                    break;
                case global::SpeedAsm.Operation.Inc:
                    result.Operation = Operation.INC;
                    result.CType = OperandType.None;
                    break;
                case global::SpeedAsm.Operation.Sub:
                    result.Operation = Operation.SUB;
                    break;
                case global::SpeedAsm.Operation.Dec:
                    result.Operation = Operation.DEC;
                    result.CType = OperandType.None;
                    break;
                case global::SpeedAsm.Operation.Mul:
                    result.Operation = Operation.MLT;
                    break;
                case global::SpeedAsm.Operation.Div:
                    result.Operation = Operation.DIV;
                    break;
                case global::SpeedAsm.Operation.Mod:
                    result.Operation = Operation.MOD;
                    break;
                case global::SpeedAsm.Operation.And:
                    result.Operation = Operation.AND;
                    break;
                case global::SpeedAsm.Operation.Or:
                    result.Operation = Operation.OR;
                    break;
                case global::SpeedAsm.Operation.Xor:
                    result.Operation = Operation.XOR;
                    break;
                case global::SpeedAsm.Operation.Not:
                    result.Operation = Operation.NOT;
                    result.CType = OperandType.None;
                    break;
                case global::SpeedAsm.Operation.BAnd:
                    {
                        var skipB = new Label();
                        var skipC = new Label();
                        Urcl.AddRange(new[]
                        {
                            new UrclInstruction(Operation.OR, OperandType.Register, result.B, OperandType.Register, result.B, OperandType.Register, result.B),
                            new UrclInstruction(Operation.BRZ, skipB),
                            new UrclInstruction(Operation.IMM, OperandType.Register, result.B, OperandType.Immediate, 1),
                            new UrclInstruction(Operation.COMPILER_MARKLABEL, skipB),
                            new UrclInstruction(Operation.OR, OperandType.Register, result.C, OperandType.Register, result.C, OperandType.Register, result.C),
                            new UrclInstruction(Operation.BRZ, skipC),
                            new UrclInstruction(Operation.IMM, OperandType.Register, result.C, OperandType.Immediate, 1),
                            new UrclInstruction(Operation.COMPILER_MARKLABEL, skipC),
                            new UrclInstruction(Operation.AND, OperandType.Register, result.A, OperandType.Register, result.B, OperandType.Register, result.C)
                        });
                    }
                    break;
                case global::SpeedAsm.Operation.BOr:
                    {
                        var skipB = new Label();
                        var skipC = new Label();
                        Urcl.AddRange(new[]
                        {
                            new UrclInstruction(Operation.OR, OperandType.Register, result.B, OperandType.Register, result.B, OperandType.Register, result.B),
                            new UrclInstruction(Operation.BRZ, skipB),
                            new UrclInstruction(Operation.IMM, OperandType.Register, result.B, OperandType.Immediate, 1),
                            new UrclInstruction(Operation.COMPILER_MARKLABEL, skipB),
                            new UrclInstruction(Operation.OR, OperandType.Register, result.C, OperandType.Register, result.C, OperandType.Register, result.C),
                            new UrclInstruction(Operation.BRZ, skipC),
                            new UrclInstruction(Operation.IMM, OperandType.Register, result.C, OperandType.Immediate, 1),
                            new UrclInstruction(Operation.COMPILER_MARKLABEL, skipC),
                            new UrclInstruction(Operation.OR, OperandType.Register, result.A, OperandType.Register, result.B, OperandType.Register, result.C)
                        });
                    }
                    break;
                case global::SpeedAsm.Operation.BXor:
                    {
                        var skipB = new Label();
                        var skipC = new Label();
                        Urcl.AddRange(new[]
                        {
                            new UrclInstruction(Operation.OR, OperandType.Register, result.B, OperandType.Register, result.B, OperandType.Register, result.B),
                            new UrclInstruction(Operation.BRZ, skipB),
                            new UrclInstruction(Operation.IMM, OperandType.Register, result.B, OperandType.Immediate, 1),
                            new UrclInstruction(Operation.COMPILER_MARKLABEL, skipB),
                            new UrclInstruction(Operation.OR, OperandType.Register, result.C, OperandType.Register, result.C, OperandType.Register, result.C),
                            new UrclInstruction(Operation.BRZ, skipC),
                            new UrclInstruction(Operation.IMM, OperandType.Register, result.C, OperandType.Immediate, 1),
                            new UrclInstruction(Operation.COMPILER_MARKLABEL, skipC),
                            new UrclInstruction(Operation.XOR, OperandType.Register, result.A, OperandType.Register, result.B, OperandType.Register, result.C)
                        });
                    }
                    break;
                case global::SpeedAsm.Operation.BNot:
                    {
                        var skipB = new Label();
                        var end = new Label();
                        Urcl.AddRange(new[]
                        {
                            new UrclInstruction(Operation.OR, OperandType.Register, result.B, OperandType.Register, result.B, OperandType.Register, result.B),
                            new UrclInstruction(Operation.BNZ, skipB),
                            new UrclInstruction(Operation.IMM, OperandType.Register, result.A, OperandType.Immediate, 1),
                            new UrclInstruction(Operation.BRA, end),
                            new UrclInstruction(Operation.COMPILER_MARKLABEL, skipB),
                            new UrclInstruction(Operation.MOV, OperandType.Register, result.A, OperandType.Register, 0),
                            new UrclInstruction(Operation.COMPILER_MARKLABEL, end),
                            new UrclInstruction(Operation.OR, OperandType.Register, result.A, OperandType.Register, result.A, OperandType.Register, result.A)
                        });
                    }
                    break;
                case global::SpeedAsm.Operation.Label:
                    result.Operation = Operation.COMPILER_MARKLABEL;
                    result.AType = OperandType.Label;
                    result.ALabel = GetLabel(result.A);
                    result.BType = OperandType.None;
                    result.CType = OperandType.None;
                    break;
                case global::SpeedAsm.Operation.Branch:
                    result.Operation = Operation.BRA;
                    result.AType = OperandType.Label;
                    result.ALabel = GetLabel(result.A);
                    result.BType = OperandType.None;
                    result.CType = OperandType.None;
                    break;
                case global::SpeedAsm.Operation.BranchIfNotZero:
                    result.Operation = Operation.BNZ;
                    result.AType = OperandType.Label;
                    result.ALabel = GetLabel(result.A);
                    result.BType = OperandType.None;
                    result.CType = OperandType.None;
                    break;
                case global::SpeedAsm.Operation.BranchIfZero:
                    result.Operation = Operation.BRZ;
                    result.AType = OperandType.Label;
                    result.ALabel = GetLabel(result.A);
                    result.BType = OperandType.None;
                    result.CType = OperandType.None;
                    break;
                case global::SpeedAsm.Operation.BranchIfCarry:
                    result.Operation = Operation.BRC;
                    result.AType = OperandType.Label;
                    result.ALabel = GetLabel(result.A);
                    result.BType = OperandType.None;
                    result.CType = OperandType.None;
                    break;
                case global::SpeedAsm.Operation.BranchIfSign:
                    result.Operation = Operation.BRN;
                    result.AType = OperandType.Label;
                    result.ALabel = GetLabel(result.A);
                    result.BType = OperandType.None;
                    result.CType = OperandType.None;
                    break;
                case global::SpeedAsm.Operation.BranchIfNotCarry:
                    result.Operation = Operation.BNC;
                    result.AType = OperandType.Label;
                    result.ALabel = GetLabel(result.A);
                    result.BType = OperandType.None;
                    result.CType = OperandType.None;
                    break;
                case global::SpeedAsm.Operation.BranchIfNotSign:
                    result.Operation = Operation.BRN;
                    result.AType = OperandType.Label;
                    result.ALabel = GetLabel(result.A);
                    result.BType = OperandType.None;
                    result.CType = OperandType.None;
                    break;
                default:
                    throw new CompileError($"Operation is not supported for URCL: {inst.Operation}");
            }

            if (result.Operation != Operation.NOP) Urcl.Add(result);

            if (Urcl.Count > start)
            {
                foreach (var urcl in Urcl.GetRange(start, Urcl.Count - start))
                {
                    Instructions.Enqueue(urcl);
                }
            }
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, Urcl);
        }

        private Label GetLabel(ulong id)
        {
            if (!Labels.TryGetValue(id, out Label label))
            {
                label = new Label();
                Labels.Add(id, label);
            }

            return label;
        }
    }
}
