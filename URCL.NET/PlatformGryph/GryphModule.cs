using System;
using System.Collections.Generic;

namespace URCL.NET.PlatformGryph
{
    public class GryphModule
    {
        public const string NameFileGry = "Gryph Binary";

        public void HandleEmitGry(Action<byte> emit, IEnumerable<UrclInstruction> instructions)
        {
            var maxReg = 0UL;

            foreach (var inst in instructions)
            {
                if (maxReg < inst.MaxRegister) maxReg = inst.MaxRegister;
            }

            if (maxReg > 256) throw new Exception($"Cannot allocate enough RAM to support {maxReg} registers.");

            var expanded = new List<UrclInstruction>();
            var maxRam = 256 - (long)maxReg;

            foreach (var instruction in instructions)
            {
                var inst = instruction.Clone();

                if (inst.AType == OperandType.Register) inst.A += 16;
                if (inst.BType == OperandType.Register) inst.B += 16;
                if (inst.CType == OperandType.Register) inst.C += 16;

                expanded.AddRange(Expand(inst, maxRam));
            }

            if (expanded.Count > 256) throw new Exception($"Cannot allocate enough ROM to support {expanded.Count} ISA instructions.");

            var labels = new Dictionary<Label, byte>();

            for (int i = 0; i < expanded.Count; i++)
            {
                var inst = expanded[i];

                if (inst.Operation == Operation.COMPILER_MARKLABEL)
                {
                    labels[inst.ALabel] = (byte)i;
                    expanded.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < expanded.Count; i++)
            {
                var inst = expanded[i];

                if (inst.AType == OperandType.Label)
                {
                    if (labels.TryGetValue(inst.ALabel, out byte address))
                    {
                        inst.A = address;
                        inst.AType = OperandType.Immediate;
                    }
                    else
                    {
                        throw new Exception("Undefined label cannot be resolved.");
                    }
                }

                if (inst.BType == OperandType.Label)
                {
                    if (labels.TryGetValue(inst.BLabel, out byte address))
                    {
                        inst.B = address;
                        inst.BType = OperandType.Immediate;
                    }
                    else
                    {
                        throw new Exception("Undefined label cannot be resolved.");
                    }
                }

                if (inst.CType == OperandType.Label)
                {
                    if (labels.TryGetValue(inst.CLabel, out byte address))
                    {
                        inst.C = address;
                        inst.CType = OperandType.Immediate;
                    }
                    else
                    {
                        throw new Exception("Undefined label cannot be resolved.");
                    }
                }
            }

            Translate(emit, expanded);
        }

        private void Translate(Action<byte> emit, IEnumerable<UrclInstruction> instructions)
        {
            var used = 0;

            foreach (var inst in instructions)
            {
                switch (inst.Operation)
                {
                    case Operation.MOV:
                        if (used % 2 != 0)
                        {
                            emit(0);
                            used += 1;
                        }
                        new Op((ReadRegister)inst.B, (WriteRegister)inst.A).Emit(emit);
                        used += 2;
                        break;
                    case Operation.IMM:
                        if (used % 2 != 0)
                        {
                            emit(0);
                            used += 1;
                        }
                        new Op(ReadRegister.RIMM, (WriteRegister)inst.A, (byte)inst.B).Emit(emit);
                        used += 2;
                        break;
                    case Operation.DW:
                        emit((byte)inst.A);
                        used += 1;
                        break;
                    case Operation.DD:
                        emit((byte)inst.A);
                        emit((byte)(inst.A >> 8));
                        used += 2;
                        break;
                    case Operation.DQ:
                        emit((byte)inst.A);
                        emit((byte)(inst.A >> 8));
                        emit((byte)(inst.A >> 16));
                        emit((byte)(inst.A >> 24));
                        used += 4;
                        break;
                    default:
                        throw new Exception($"Operation {inst.Operation} can not be directly translated to the target architecture.");
                }
            }
        }

        private UrclInstruction[] Expand(UrclInstruction inst, long maxRam)
        {
            var expanded = new List<UrclInstruction>();

            switch (inst.Operation)
            {
                case Operation.NOP:
                    expanded.AddRange(Move(WriteRegister.A, ReadRegister.A));
                    break;
                case Operation.BRK:
                case Operation.HLT:
                    expanded.AddRange(Move(WriteRegister.RIP, ReadRegister.RIP));
                    break;
                case Operation.ADD:
                    expanded.AddRange(Get(WriteRegister.RADDA, inst, 1));
                    expanded.AddRange(Get(WriteRegister.RADDB, inst, 2));
                    expanded.AddRange(Move(inst.A, ReadRegister.RADD));
                    expanded.AddRange(Move(WriteRegister.A, ReadRegister.RADD));
                    break;
                case Operation.INC:
                    expanded.AddRange(Get(WriteRegister.RADDA, inst, 1));
                    expanded.AddRange(Immediate(WriteRegister.RADDB, 1));
                    expanded.AddRange(Move(inst.A, ReadRegister.RADD));
                    expanded.AddRange(Move(WriteRegister.A, ReadRegister.RADD));
                    break;
                case Operation.SUB:
                    expanded.AddRange(Get(WriteRegister.RADDA, inst, 1));
                    expanded.AddRange(Get(WriteRegister.RXORA, inst, 2));
                    expanded.AddRange(Immediate(WriteRegister.RXORB, ulong.MaxValue));
                    expanded.AddRange(Move(WriteRegister.RADDB, ReadRegister.RXOR));
                    expanded.AddRange(Immediate(WriteRegister.RCF, 1));
                    expanded.AddRange(Move(inst.A, ReadRegister.RADD));
                    expanded.AddRange(Move(WriteRegister.A, ReadRegister.RADD));
                    break;
                case Operation.DEC:
                    expanded.AddRange(Get(WriteRegister.RADDA, inst, 1));
                    expanded.AddRange(Immediate(WriteRegister.RADDB, ulong.MaxValue));
                    expanded.AddRange(Immediate(WriteRegister.RCF, 1));
                    expanded.AddRange(Move(inst.A, ReadRegister.RADD));
                    expanded.AddRange(Move(WriteRegister.A, ReadRegister.RADD));
                    break;
                case Operation.CMP:
                    expanded.AddRange(Get(WriteRegister.RADDA, inst, 1));
                    expanded.AddRange(Get(WriteRegister.RXORA, inst, 2));
                    expanded.AddRange(Immediate(WriteRegister.RXORB, ulong.MaxValue));
                    expanded.AddRange(Move(WriteRegister.RADDB, ReadRegister.RXOR));
                    expanded.AddRange(Immediate(WriteRegister.RCF, 1));
                    expanded.AddRange(Move(WriteRegister.A, ReadRegister.RADD));
                    break;
                case Operation.AND:
                    expanded.AddRange(Get(WriteRegister.RANDORA, inst, 1));
                    expanded.AddRange(Get(WriteRegister.RANDORB, inst, 2));
                    expanded.AddRange(Move(inst.A, ReadRegister.RAND));
                    expanded.AddRange(Move(WriteRegister.A, ReadRegister.RAND));
                    expanded.AddRange(Move(WriteRegister.RADDA, ReadRegister.RZERO));
                    expanded.AddRange(Move(WriteRegister.RADDB, ReadRegister.RZERO));
                    break;
                case Operation.OR:
                    expanded.AddRange(Get(WriteRegister.RANDORA, inst, 1));
                    expanded.AddRange(Get(WriteRegister.RANDORB, inst, 2));
                    expanded.AddRange(Move(inst.A, ReadRegister.ROR));
                    expanded.AddRange(Move(WriteRegister.A, ReadRegister.ROR));
                    expanded.AddRange(Move(WriteRegister.RADDA, ReadRegister.RZERO));
                    expanded.AddRange(Move(WriteRegister.RADDB, ReadRegister.RZERO));
                    break;
                case Operation.XOR:
                    expanded.AddRange(Get(WriteRegister.RXORA, inst, 1));
                    expanded.AddRange(Get(WriteRegister.RXORB, inst, 2));
                    expanded.AddRange(Move(inst.A, ReadRegister.RXOR));
                    expanded.AddRange(Move(WriteRegister.A, ReadRegister.RXOR));
                    expanded.AddRange(Move(WriteRegister.RADDA, ReadRegister.RZERO));
                    expanded.AddRange(Move(WriteRegister.RADDB, ReadRegister.RZERO));
                    break;
                case Operation.NAND:
                    expanded.AddRange(Get(WriteRegister.RANDORA, inst, 1));
                    expanded.AddRange(Get(WriteRegister.RANDORB, inst, 2));
                    expanded.AddRange(Move(WriteRegister.RXORA, ReadRegister.RAND));
                    expanded.AddRange(Immediate(WriteRegister.RXORB, ulong.MaxValue));
                    expanded.AddRange(Move(inst.A, ReadRegister.RXOR));
                    expanded.AddRange(Move(WriteRegister.A, ReadRegister.RXOR));
                    expanded.AddRange(Move(WriteRegister.RADDA, ReadRegister.RZERO));
                    expanded.AddRange(Move(WriteRegister.RADDB, ReadRegister.RZERO));
                    break;
                case Operation.NOR:
                    expanded.AddRange(Get(WriteRegister.RANDORA, inst, 1));
                    expanded.AddRange(Get(WriteRegister.RANDORB, inst, 2));
                    expanded.AddRange(Move(WriteRegister.RXORA, ReadRegister.ROR));
                    expanded.AddRange(Immediate(WriteRegister.RXORB, ulong.MaxValue));
                    expanded.AddRange(Move(inst.A, ReadRegister.RXOR));
                    expanded.AddRange(Move(WriteRegister.A, ReadRegister.RXOR));
                    expanded.AddRange(Move(WriteRegister.RADDA, ReadRegister.RZERO));
                    expanded.AddRange(Move(WriteRegister.RADDB, ReadRegister.RZERO));
                    break;
                case Operation.XNOR:
                    expanded.AddRange(Get(WriteRegister.RXORA, inst, 1));
                    expanded.AddRange(Get(WriteRegister.RXORB, inst, 2));
                    expanded.AddRange(Move(WriteRegister.RXORA, ReadRegister.RXOR));
                    expanded.AddRange(Immediate(WriteRegister.RXORB, ulong.MaxValue));
                    expanded.AddRange(Move(inst.A, ReadRegister.RXOR));
                    expanded.AddRange(Move(WriteRegister.A, ReadRegister.RXOR));
                    expanded.AddRange(Move(WriteRegister.RADDA, ReadRegister.RZERO));
                    expanded.AddRange(Move(WriteRegister.RADDB, ReadRegister.RZERO));
                    break;
                case Operation.NOT:
                    expanded.AddRange(Get(WriteRegister.RXORA, inst, 1));
                    expanded.AddRange(Immediate(WriteRegister.RXORB, ulong.MaxValue));
                    expanded.AddRange(Move(inst.A, ReadRegister.RXOR));
                    expanded.AddRange(Move(WriteRegister.A, ReadRegister.RXOR));
                    expanded.AddRange(Move(WriteRegister.RADDA, ReadRegister.RZERO));
                    expanded.AddRange(Move(WriteRegister.RADDB, ReadRegister.RZERO));
                    break;
                case Operation.LSH:
                    expanded.AddRange(Get(WriteRegister.RSHA, inst, 1));
                    expanded.AddRange(Immediate(WriteRegister.RSHA, 1));
                    expanded.AddRange(Move(inst.A, ReadRegister.RLSH));
                    expanded.AddRange(Move(WriteRegister.A, ReadRegister.RLSH));
                    expanded.AddRange(Move(WriteRegister.RADDA, ReadRegister.RZERO));
                    expanded.AddRange(Move(WriteRegister.RADDB, ReadRegister.RZERO));
                    break;
                case Operation.BSL:
                    expanded.AddRange(Get(WriteRegister.RSHA, inst, 1));
                    expanded.AddRange(Get(WriteRegister.RSHA, inst, 2));
                    expanded.AddRange(Move(inst.A, ReadRegister.RLSH));
                    expanded.AddRange(Move(WriteRegister.A, ReadRegister.RLSH));
                    expanded.AddRange(Move(WriteRegister.RADDA, ReadRegister.RZERO));
                    expanded.AddRange(Move(WriteRegister.RADDB, ReadRegister.RZERO));
                    break;
                case Operation.RSH:
                    expanded.AddRange(Get(WriteRegister.RSHA, inst, 1));
                    expanded.AddRange(Immediate(WriteRegister.RSHA, 1));
                    expanded.AddRange(Move(inst.A, ReadRegister.RRSH));
                    expanded.AddRange(Move(WriteRegister.A, ReadRegister.RRSH));
                    expanded.AddRange(Move(WriteRegister.RADDA, ReadRegister.RZERO));
                    expanded.AddRange(Move(WriteRegister.RADDB, ReadRegister.RZERO));
                    break;
                case Operation.BSR:
                    expanded.AddRange(Get(WriteRegister.RSHA, inst, 1));
                    expanded.AddRange(Get(WriteRegister.RSHA, inst, 2));
                    expanded.AddRange(Move(inst.A, ReadRegister.RRSH));
                    expanded.AddRange(Move(WriteRegister.A, ReadRegister.RRSH));
                    expanded.AddRange(Move(WriteRegister.RADDA, ReadRegister.RZERO));
                    expanded.AddRange(Move(WriteRegister.RADDB, ReadRegister.RZERO));
                    break;
                case Operation.MOV:
                    expanded.AddRange(Move(inst.A, inst.B));
                    break;
                case Operation.IMM:
                    if (inst.BType == OperandType.Label)
                    {
                        expanded.AddRange(Immediate(inst.A, inst.BLabel));
                    }
                    else
                    {
                        expanded.AddRange(Immediate(inst.A, inst.B));
                    }
                    break;
                case Operation.LOD:
                    expanded.AddRange(Get(WriteRegister.RADDR, inst, 1));
                    expanded.AddRange(Move(inst.A, ReadRegister.RMEM));
                    break;
                case Operation.STR:
                    expanded.AddRange(Get(WriteRegister.RADDR, inst, 0));
                    expanded.AddRange(Get(WriteRegister.RMEM, inst, 1));
                    break;
                case Operation.BRA:
                    expanded.AddRange(Get(WriteRegister.RIP, inst, 0));
                    break;
                case Operation.BRZ:
                    expanded.AddRange(Get(WriteRegister.B, inst, 0));
                    expanded.AddRange(BranchIfZero(ReadRegister.A, ReadRegister.B));
                    break;
                case Operation.BNZ:
                    expanded.AddRange(Move(WriteRegister.RCF, ReadRegister.A));
                    expanded.AddRange(Immediate(WriteRegister.RADDA, ulong.MaxValue));
                    expanded.AddRange(Move(WriteRegister.RADDB, ReadRegister.RZERO));
                    expanded.AddRange(Move(WriteRegister.B, ReadRegister.RADD));
                    expanded.AddRange(Get(WriteRegister.C, inst, 0));
                    expanded.AddRange(BranchIfZero(ReadRegister.B, ReadRegister.C));
                    break;
                case Operation.BRC:
                    expanded.AddRange(Move(WriteRegister.RXORA, ReadRegister.RCF));
                    expanded.AddRange(Immediate(WriteRegister.RXORB, 1));
                    expanded.AddRange(Move(WriteRegister.B, ReadRegister.RXOR));
                    expanded.AddRange(Get(WriteRegister.C, inst, 0));
                    expanded.AddRange(BranchIfZero(ReadRegister.B, ReadRegister.C));
                    break;
                case Operation.BNC:
                    expanded.AddRange(Move(WriteRegister.B, ReadRegister.RCF));
                    expanded.AddRange(Get(WriteRegister.C, inst, 0));
                    expanded.AddRange(BranchIfZero(ReadRegister.B, ReadRegister.C));
                    break;
                case Operation.BRP:
                    expanded.AddRange(Move(WriteRegister.RANDORA, ReadRegister.A));
                    expanded.AddRange(Immediate(WriteRegister.RANDORB, 0b10000000));
                    expanded.AddRange(Move(WriteRegister.B, ReadRegister.RAND));
                    expanded.AddRange(Get(WriteRegister.C, inst, 0));
                    expanded.AddRange(BranchIfZero(ReadRegister.B, ReadRegister.C));
                    break;
                case Operation.BRN:
                    expanded.AddRange(Move(WriteRegister.RANDORA, ReadRegister.A));
                    expanded.AddRange(Immediate(WriteRegister.RANDORB, 0b10000000));
                    expanded.AddRange(Move(WriteRegister.RXORA, ReadRegister.RAND));
                    expanded.AddRange(Immediate(WriteRegister.RXORB, 0b1000000));
                    expanded.AddRange(Move(WriteRegister.B, ReadRegister.RXOR));
                    expanded.AddRange(Get(WriteRegister.C, inst, 0));
                    expanded.AddRange(BranchIfZero(ReadRegister.B, ReadRegister.C));
                    break;
                case Operation.BEV:
                    expanded.AddRange(Move(WriteRegister.RANDORA, ReadRegister.A));
                    expanded.AddRange(Immediate(WriteRegister.RANDORB, 1));
                    expanded.AddRange(Move(WriteRegister.B, ReadRegister.RAND));
                    expanded.AddRange(Get(WriteRegister.C, inst, 0));
                    expanded.AddRange(BranchIfZero(ReadRegister.B, ReadRegister.C));
                    break;
                case Operation.BOD:
                    expanded.AddRange(Move(WriteRegister.RANDORA, ReadRegister.A));
                    expanded.AddRange(Immediate(WriteRegister.RANDORB, 1));
                    expanded.AddRange(Move(WriteRegister.RXORA, ReadRegister.RAND));
                    expanded.AddRange(Immediate(WriteRegister.RXORB, 1));
                    expanded.AddRange(Move(WriteRegister.B, ReadRegister.RXOR));
                    expanded.AddRange(Get(WriteRegister.C, inst, 0));
                    expanded.AddRange(BranchIfZero(ReadRegister.B, ReadRegister.C));
                    break;
                case Operation.DW:
                    expanded.Add(inst);
                    break;
                case Operation.DD:
                    expanded.Add(inst);
                    break;
                case Operation.DQ:
                    expanded.Add(inst);
                    break;
                case Operation.MINRAM:
                    if ((long)inst.A > maxRam || (long)inst.A < 0)
                    {
                        throw new Exception($"Minimum RAM requirement not met. {maxRam}/{inst.A} bytes available.");
                    }
                    break;
                case Operation.BITS:
                    if (inst.A != 8) throw new Exception("Requested word bit length does not match target architecture.");
                    break;
                case Operation.COMPILER_CREATELABEL:
                    break;
                case Operation.COMPILER_MARKLABEL:
                    expanded.Add(inst);
                    break;
                case Operation.COMPILER_COMMENT:
                    break;
                default:
                    throw new Exception($"Operation \"{inst.Operation}\" is not supported by the target architecture.");
            }

            return expanded.ToArray();
        }

        private UrclInstruction[] BranchIfZero(ReadRegister value, ReadRegister target)
        {
            var expanded = new List<UrclInstruction>();
            expanded.AddRange(Move(WriteRegister.RCF, value));
            expanded.AddRange(Immediate(WriteRegister.RADDA, ulong.MaxValue));
            expanded.AddRange(Move(WriteRegister.RADDB, ReadRegister.RZERO));
            expanded.AddRange(Move(WriteRegister.RANDORA, ReadRegister.RADD));
            expanded.AddRange(Move(WriteRegister.RXORA, ReadRegister.RADD));
            expanded.AddRange(Immediate(WriteRegister.RXORB, ulong.MaxValue));
            expanded.AddRange(Move(WriteRegister.RANDORB, target));
            expanded.AddRange(Move(WriteRegister.B, ReadRegister.RAND));
            expanded.AddRange(Move(WriteRegister.RCF, ReadRegister.RZERO));
            expanded.AddRange(Move(WriteRegister.RADDA, ReadRegister.RIP));
            expanded.AddRange(Immediate(WriteRegister.RADDB, 7));
            expanded.AddRange(Move(WriteRegister.RANDORA, ReadRegister.RADD));
            expanded.AddRange(Move(WriteRegister.RANDORB, ReadRegister.RXOR));
            expanded.AddRange(Move(WriteRegister.RANDORA, ReadRegister.RAND));
            expanded.AddRange(Move(WriteRegister.RANDORB, ReadRegister.B));
            expanded.AddRange(Move(WriteRegister.RIP, ReadRegister.ROR));
            return expanded.ToArray();
        }

        private UrclInstruction[] Get(WriteRegister a, UrclInstruction inst, byte index)
        {
            return index switch
            {
                0 => inst.AType switch
                {
                    OperandType.None => throw new IndexOutOfRangeException(),
                    OperandType.Register => Move(a, inst.A),
                    OperandType.Immediate => Immediate(a, inst.A),
                    OperandType.Label => Immediate(a, inst.ALabel),
                    _ => throw new InvalidOperationException(),
                },
                1 => inst.BType switch
                {
                    OperandType.None => throw new IndexOutOfRangeException(),
                    OperandType.Register => Move(a, inst.B),
                    OperandType.Immediate => Immediate(a, inst.B),
                    OperandType.Label => Immediate(a, inst.BLabel),
                    _ => throw new InvalidOperationException(),
                },
                2 => inst.CType switch
                {
                    OperandType.None => throw new IndexOutOfRangeException(),
                    OperandType.Register => Move(a, inst.C),
                    OperandType.Immediate => Immediate(a, inst.C),
                    OperandType.Label => Immediate(a, inst.CLabel),
                    _ => throw new InvalidOperationException(),
                },
                _ => throw new IndexOutOfRangeException(),
            };
        }

        private UrclInstruction[] Immediate(WriteRegister a, Label imm)
        {
            return Immediate((ulong)a, imm);
        }

        private UrclInstruction[] Immediate(ulong a, Label imm)
        {
            if (a == 16)
            {
                return new UrclInstruction[0];
            }
            else if (a <= 0xF)
            {
                return new[] { new UrclInstruction(Operation.IMM, OperandType.Register, a, imm) };
            }
            else
            {
                return new[]
                {
                    new UrclInstruction(Operation.IMM, OperandType.Register, (ulong)WriteRegister.RADDR, OperandType.Immediate, 256 - (a - 16)),
                    new UrclInstruction(Operation.IMM, OperandType.Register, (ulong)WriteRegister.RMEM, imm)
                };
            }
        }

        private UrclInstruction[] Immediate(WriteRegister a, ulong imm)
        {
            return Immediate((ulong)a, imm);
        }

        private UrclInstruction[] Immediate(ulong a, ulong imm)
        {
            if (a == 16)
            {
                return new UrclInstruction[0];
            }
            else if (a <= 0xF)
            {
                return new[] { new UrclInstruction(Operation.IMM, OperandType.Register, a, OperandType.Immediate, imm) };
            }
            else
            {
                return new[]
                {
                    new UrclInstruction(Operation.IMM, OperandType.Register, (ulong)WriteRegister.RADDR, OperandType.Immediate, 256 - (a - 16)),
                    new UrclInstruction(Operation.IMM, OperandType.Register, (ulong)WriteRegister.RMEM, OperandType.Immediate, imm)
                };
            }
        }

        private UrclInstruction[] Move(WriteRegister a, ReadRegister b)
        {
            return Move((ulong)a, (ulong)b);
        }

        private UrclInstruction[] Move(ulong a, ReadRegister b)
        {
            return Move(a, (ulong)b);
        }

        private UrclInstruction[] Move(WriteRegister a, ulong b)
        {
            return Move((ulong)a, b);
        }

        private UrclInstruction[] Move(ulong a, ulong b)
        {
            if (b == 16) b = (ulong)ReadRegister.RZERO;

            if (a == 16)
            {
                return new UrclInstruction[0];
            }
            else if (a <= 0xF && b <= 0xF)
            {
                return new[] { new UrclInstruction(Operation.MOV, OperandType.Register, a, OperandType.Register, b) };
            }
            else if (a <= 0xF)
            {
                return new[]
                {
                    new UrclInstruction(Operation.IMM, OperandType.Register, (ulong)WriteRegister.RADDR, OperandType.Immediate, 256 - (b - 16)),
                    new UrclInstruction(Operation.MOV, OperandType.Register, a, OperandType.Register, (ulong)ReadRegister.RMEM)
                };
            }
            else if (b <= 0xF)
            {
                return new[]
                {
                    new UrclInstruction(Operation.IMM, OperandType.Register, (ulong)WriteRegister.RADDR, OperandType.Immediate, 256 - (a - 16)),
                    new UrclInstruction(Operation.MOV, OperandType.Register, (ulong)WriteRegister.RMEM, OperandType.Register, b)
                };
            }
            else
            {
                return new[]
                {
                    new UrclInstruction(Operation.IMM, OperandType.Register, (ulong)WriteRegister.RADDR, OperandType.Immediate, 256 - (b - 0xF)),
                    new UrclInstruction(Operation.MOV, OperandType.Register, (ulong)WriteRegister.B, OperandType.Register, (ulong)ReadRegister.RMEM),
                    new UrclInstruction(Operation.IMM, OperandType.Register, (ulong)WriteRegister.RADDR, OperandType.Immediate, 256 - (a - 0xF)),
                    new UrclInstruction(Operation.MOV, OperandType.Register, (ulong)WriteRegister.RMEM, OperandType.Register, (ulong)ReadRegister.B)
                };
            }
        }

        private struct Op
        {
            public ReadRegister Read;
            public WriteRegister Write;
            public byte Immediate;

            public Op(ReadRegister read, WriteRegister write, byte imm = 0)
            {
                Read = read;
                Write = write;
                Immediate = imm;
            }

            public void Emit(Action<byte> emit)
            {
                emit((byte)(((byte)Read << 4) | (byte)Write));
                emit(Immediate);
            }
        }

        private enum ReadRegister : byte
        {
            //Zero
            RZERO = 0x0,
            //Add Result
            RADD = 0x1,
            //Add Carry Out
            RCF = 0x2,
            //Left Shift Result
            RLSH = 0x3,
            //Right Shift Result
            RRSH = 0x4,
            //And Result
            RAND = 0x5,
            //Or Result
            ROR = 0x6,
            //Xor Result
            RXOR = 0x7,
            //One
            RIMM = 0x8,
            //General A
            A = 0x9,
            //General B
            B = 0xA,
            //General C
            C = 0xB,
            //General D
            D = 0xC,
            //Memory Value
            RMEM = 0xD,
            //Memory Address
            RADDR = 0xE,
            //Instruction Pointer
            RIP = 0xF
        }

        private enum WriteRegister : byte
        {
            //Add Carry In
            RCF = 0x0,
            //Add A
            RADDA = 0x1,
            //Add B
            RADDB = 0x2,
            //Shift A
            RSHA = 0x3,
            //Shift B
            RSHB = 0x4,
            //And/Or A
            RANDORA = 0x5,
            //And/Or B
            RANDORB = 0x6,
            //Xor A
            RXORA = 0x7,
            //Xor B
            RXORB = 0x8,
            //General A
            A = 0x9,
            //General B
            B = 0xA,
            //General C
            C = 0xB,
            //General D
            D = 0xC,
            //Memory Value
            RMEM = 0xD,
            //Memory Address
            RADDR = 0xE,
            //Instruction Pointer
            RIP = 0xF
        }
    }
}
