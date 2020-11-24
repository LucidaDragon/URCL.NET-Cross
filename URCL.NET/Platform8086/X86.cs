using System;
using System.Collections.Generic;

namespace URCL.NET.Platform8086
{
    public class X86 : IGenerator
    {
        private readonly Operand AX = Operand.Reg("ax");
        private readonly Operand BX = Operand.Reg("bx");
        private readonly Operand CX = Operand.Reg("cx");
        private readonly Operand DX = Operand.Reg("dx");

        private const ulong RegistersOffset = 0x500;
        private const ulong StackTop = 0x7BFF;
        private ulong Registers = 0;
        private ulong StackBottom => RegistersOffset + (Registers * 2);

        private const string StartOfHeap = "___HeapStart___";

        public string CommentPrefix { get; set; } = ";";

        public void Generate(Action<string> emit, UrclInstruction[] instructions)
        {
            Registers = 0;

            foreach (var inst in instructions)
            {
                var max = inst.MaxRegister;

                if (max > Registers) Registers = max;
            }

            var x86 = new List<X86Inst>(instructions.Length)
            {
                new X86Inst("org 0x7C00")
            };

            Set(x86.Add, Operand.Reg("sp"), Operand.Imm(StackTop.ToString()));
            Set(x86.Add, Operand.Reg("bp"), Operand.Imm(StackBottom.ToString()));

            foreach (var inst in instructions)
            {
                if (inst.Operation != Operation.COMPILER_COMMENT &&
                    inst.Operation != Operation.COMPILER_MARKLABEL) x86.Add(new X86Inst($"{CommentPrefix}URCL - {inst.Operation}"));
                Emit(x86.Add, inst);
            }

            x86.Add(new X86Inst($"{StartOfHeap}:"));

            foreach (var inst in x86)
            {
                emit(inst.ToString());
            }
        }

        private void Emit(Action<X86Inst> emit, UrclInstruction instruction)
        {
            switch (instruction.Operation)
            {
                case Operation.NOP:
                    emit(new X86Inst("nop"));
                    break;
                case Operation.BRK:
                case Operation.HLT:
                    emit(new X86Inst("hlt"));
                    break;
                case Operation.ADD:
                    Set(emit, AX, GetB(instruction));
                    Set(emit, BX, GetC(instruction));
                    emit(new X86Inst("add", AX, BX));
                    Set(emit, GetA(instruction), AX);
                    break;
                case Operation.INC:
                    Set(emit, AX, GetB(instruction));
                    emit(new X86Inst("inc", AX));
                    Set(emit, GetA(instruction), AX);
                    break;
                case Operation.SUB:
                    Set(emit, AX, GetB(instruction));
                    Set(emit, BX, GetC(instruction));
                    emit(new X86Inst("sub", AX, BX));
                    Set(emit, GetA(instruction), AX);
                    break;
                case Operation.DEC:
                    Set(emit, AX, GetB(instruction));
                    emit(new X86Inst("dec", AX));
                    Set(emit, GetA(instruction), AX);
                    break;
                case Operation.MLT:
                    Set(emit, AX, GetB(instruction));
                    Set(emit, BX, GetC(instruction));
                    emit(new X86Inst("mul", BX));
                    Set(emit, GetA(instruction), AX);
                    break;
                case Operation.DIV:
                    Set(emit, AX, GetB(instruction));
                    Set(emit, BX, GetC(instruction));
                    emit(new X86Inst("xor", DX, DX));
                    emit(new X86Inst("div", BX));
                    Set(emit, GetA(instruction), AX);
                    break;
                case Operation.MOD:
                    Set(emit, AX, GetB(instruction));
                    Set(emit, BX, GetC(instruction));
                    emit(new X86Inst("xor", DX, DX));
                    emit(new X86Inst("div", BX));
                    Set(emit, GetA(instruction), DX);
                    break;
                case Operation.AND:
                    Set(emit, AX, GetB(instruction));
                    Set(emit, BX, GetC(instruction));
                    emit(new X86Inst("and", AX, BX));
                    Set(emit, GetA(instruction), AX);
                    break;
                case Operation.OR:
                    Set(emit, AX, GetB(instruction));
                    Set(emit, BX, GetC(instruction));
                    emit(new X86Inst("or", AX, BX));
                    Set(emit, GetA(instruction), AX);
                    break;
                case Operation.XOR:
                    Set(emit, AX, GetB(instruction));
                    Set(emit, BX, GetC(instruction));
                    emit(new X86Inst("xor", AX, BX));
                    Set(emit, GetA(instruction), AX);
                    break;
                case Operation.NAND:
                    Set(emit, AX, GetB(instruction));
                    Set(emit, BX, GetC(instruction));
                    emit(new X86Inst("and", AX, BX));
                    emit(new X86Inst("not", AX));
                    Set(emit, GetA(instruction), AX);
                    break;
                case Operation.NOR:
                    Set(emit, AX, GetB(instruction));
                    Set(emit, BX, GetC(instruction));
                    emit(new X86Inst("or", AX, BX));
                    emit(new X86Inst("not", AX));
                    Set(emit, GetA(instruction), AX);
                    break;
                case Operation.XNOR:
                    Set(emit, AX, GetB(instruction));
                    Set(emit, BX, GetC(instruction));
                    emit(new X86Inst("xor", AX, BX));
                    emit(new X86Inst("not", AX));
                    Set(emit, GetA(instruction), AX);
                    break;
                case Operation.NOT:
                    Set(emit, AX, GetB(instruction));
                    emit(new X86Inst("not", AX));
                    Set(emit, GetA(instruction), AX);
                    break;
                case Operation.LSH:
                    Set(emit, AX, GetB(instruction));
                    Set(emit, BX, Operand.Imm("1"));
                    emit(new X86Inst("shl", AX, BX));
                    Set(emit, GetA(instruction), AX);
                    break;
                case Operation.BSL:
                    Set(emit, AX, GetB(instruction));
                    Set(emit, BX, GetC(instruction));
                    emit(new X86Inst("shl", AX, BX));
                    Set(emit, GetA(instruction), AX);
                    break;
                case Operation.RSH:
                    Set(emit, AX, GetB(instruction));
                    Set(emit, BX, Operand.Imm("1"));
                    emit(new X86Inst("shr", AX, BX));
                    Set(emit, GetA(instruction), AX);
                    break;
                case Operation.BSR:
                    Set(emit, AX, GetB(instruction));
                    Set(emit, BX, GetC(instruction));
                    emit(new X86Inst("shr", AX, BX));
                    Set(emit, GetA(instruction), AX);
                    break;
                case Operation.MOV:
                    if (instruction.BType == OperandType.Register && instruction.B == 0)
                    {
                        Set(emit, GetA(instruction), Operand.Imm("0"));
                    }
                    else
                    {
                        Set(emit, AX, GetB(instruction));
                        Set(emit, GetA(instruction), AX);
                    }
                    break;
                case Operation.IMM:
                    Set(emit, GetA(instruction), GetB(instruction));
                    break;
                case Operation.LOAD:
                    {
                        Set(emit, BX, GetB(instruction));
                        emit(new X86Inst("shl", BX, Operand.Imm("1")));
                        Set(emit, AX, Operand.Rel(BX, 0));
                        Set(emit, GetA(instruction), AX);
                    }
                    break;
                case Operation.STORE:
                    {
                        Set(emit, AX, GetB(instruction));
                        Set(emit, BX, GetA(instruction));
                        emit(new X86Inst("shl", BX, Operand.Imm("1")));
                        Set(emit, Operand.Rel(BX, 0), AX);
                    }
                    break;
                case Operation.IN:
                    {
                        var port = GetB(instruction).ToString();

                        switch (port)
                        {
                            case "3":
                            case "78":
                            case "79":
                                emit(new X86Inst("xor", AX, AX));
                                emit(new X86Inst("int", Operand.Imm("0x16")));
                                emit(new X86Inst("and", AX, Operand.Imm("0xFF")));
                                Set(emit, GetA(instruction), AX);
                                break;
                            default:
                                throw new ArgumentException($"Unsupported IO port \"{port}\"", "B");
                        }
                    }
                    break;
                case Operation.OUT:
                    {
                        var port = GetA(instruction).ToString();

                        switch (port)
                        {
                            case "3":
                            case "78":
                            case "79":
                                Set(emit, AX, Operand.Imm("0x0A00"));
                                Set(emit, BX, GetB(instruction));
                                Set(emit, CX, Operand.Imm("1"));
                                emit(new X86Inst("and", BX, Operand.Imm("0xFF")));
                                emit(new X86Inst("or", AX, BX));
                                emit(new X86Inst("xor", BX, BX));
                                emit(new X86Inst("int", Operand.Imm("0x10")));
                                break;
                            default:
                                throw new ArgumentException($"Unsupported IO port \"{port}\"", "A");
                        }
                    }
                    break;
                case Operation.PSH:
                    {
                        emit(new X86Inst("push", GetA(instruction)));
                    }
                    break;
                case Operation.POP:
                    {
                        if (instruction.AType == OperandType.Register && instruction.A != 0)
                        {
                            emit(new X86Inst("pop", GetA(instruction)));
                        }
                        else
                        {
                            emit(new X86Inst("pop", AX));
                        }
                    }
                    break;
                case Operation.BRA:
                    {
                        var target = GetA(instruction);

                        if (instruction.AType == OperandType.Register)
                        {
                            Set(emit, AX, target);
                            target = AX;
                        }

                        emit(new X86Inst("jmp", target));
                    }
                    break;
                case Operation.BRZ:
                    {
                        var target = GetA(instruction);

                        if (instruction.AType == OperandType.Register)
                        {
                            Set(emit, AX, target);
                            target = AX;
                        }

                        emit(new X86Inst("jz", target));
                    }
                    break;
                case Operation.BNZ:
                    {
                        var target = GetA(instruction);

                        if (instruction.AType == OperandType.Register)
                        {
                            Set(emit, AX, target);
                            target = AX;
                        }

                        emit(new X86Inst("jnz", target));
                    }
                    break;
                case Operation.BRC:
                    {
                        var target = GetA(instruction);

                        if (instruction.AType == OperandType.Register)
                        {
                            Set(emit, AX, target);
                            target = AX;
                        }

                        emit(new X86Inst("jc", target));
                    }
                    break;
                case Operation.BNC:
                    {
                        var target = GetA(instruction);

                        if (instruction.AType == OperandType.Register)
                        {
                            Set(emit, AX, target);
                            target = AX;
                        }

                        emit(new X86Inst("jnc", target));
                    }
                    break;
                case Operation.BRP:
                    {
                        var target = GetA(instruction);

                        if (instruction.AType == OperandType.Register)
                        {
                            Set(emit, AX, target);
                            target = AX;
                        }

                        emit(new X86Inst("jns", target));
                    }
                    break;
                case Operation.BRN:
                    {
                        var target = GetA(instruction);

                        if (instruction.AType == OperandType.Register)
                        {
                            Set(emit, AX, target);
                            target = AX;
                        }

                        emit(new X86Inst("js", target));
                    }
                    break;
                case Operation.CAL:
                    {
                        emit(new X86Inst("call", GetA(instruction)));
                    }
                    break;
                case Operation.RET:
                    emit(new X86Inst("ret"));
                    break;
                case Operation.COMPILER_MARKLABEL:
                    emit(new X86Inst($"{LabelToString(instruction.ALabel)}:"));
                    break;
                case Operation.MINRAM:
                case Operation.BENCHMARK:
                case Operation.COMPILER_CREATELABEL:
                case Operation.COMPILER_PRAGMA:
                case Operation.COMPILER_MAXREG:
                    break;
                case Operation.COMPILER_COMMENT:
                    if (CommentPrefix != null)
                    {
                        foreach (var line in instruction.Arguments)
                        {
                            emit(new X86Inst($"{CommentPrefix}{line}"));
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private static void Set(Action<X86Inst> emit, Operand target, Operand source)
        {
            emit(new X86Inst("mov", target, source));
        }

        private static Operand GetA(UrclInstruction instruction)
        {
            return instruction.AType switch
            {
                OperandType.None => Operand.Imm("0"),
                OperandType.Register => instruction.A == 0 ? Operand.Imm("0") : Operand.Abs(((instruction.A - 1) * 2) + RegistersOffset),
                OperandType.Immediate => Operand.Imm(instruction.A.ToString()),
                OperandType.Label => Operand.Imm(LabelToString(instruction.ALabel)),
                _ => throw new ArgumentException($"Unsupported operand type {instruction.AType}.", "A"),
            };
        }

        private static Operand GetB(UrclInstruction instruction)
        {
            return instruction.BType switch
            {
                OperandType.None => Operand.Imm("0"),
                OperandType.Register => instruction.B == 0 ? Operand.Imm("0") : Operand.Abs(((instruction.B - 1) * 2) + RegistersOffset),
                OperandType.Immediate => Operand.Imm(instruction.B.ToString()),
                OperandType.Label => Operand.Imm(LabelToString(instruction.BLabel)),
                _ => throw new ArgumentException($"Unsupported operand type {instruction.BType}.", "B"),
            };
        }

        private static Operand GetC(UrclInstruction instruction)
        {
            return instruction.CType switch
            {
                OperandType.None => Operand.Imm("0"),
                OperandType.Register => instruction.C == 0 ? Operand.Imm("0") : Operand.Abs(((instruction.C - 1) * 2) + RegistersOffset),
                OperandType.Immediate => Operand.Imm(instruction.C.ToString()),
                OperandType.Label => Operand.Imm(LabelToString(instruction.CLabel)),
                _ => throw new ArgumentException($"Unsupported operand type {instruction.CType}.", "C"),
            };
        }

        private static string LabelToString(Label label)
        {
            return $"L{label.Id:X}";
        }

        private struct X86Inst
        {
            private readonly string Operation;
            private readonly Operand[] Operands;

            public X86Inst(string operation, params Operand[] operands)
            {
                Operation = operation;
                Operands = operands;
            }

            public override string ToString()
            {
                return $"{Operation} {string.Join(", ", Operands)}".Trim();
            }
        }

        private struct Operand
        {
            public bool IsRegister => Register != null;
            public bool IsImmediate => Immediate != null;
            public bool IsRelAddress => RelativeAddress.HasValue;
            public bool IsAbsAddress => AbsoluteAddress.HasValue;

            private string Register;
            private string Immediate;
            private ulong? RelativeAddress;
            private ulong? AbsoluteAddress;

            public override string ToString()
            {
                if (IsImmediate)
                {
                    return Immediate;
                }
                else if (IsRelAddress)
                {
                    if (IsRegister)
                    {
                        return $"[{Register}+0x{RelativeAddress:X}+{StartOfHeap}]";
                    }
                    else
                    {
                        return $"[0x{RelativeAddress:X}+{StartOfHeap}]";
                    }
                }
                else if (IsAbsAddress)
                {
                    if (IsRegister)
                    {
                        return $"[{Register}+0x{AbsoluteAddress:X}]";
                    }
                    else
                    {
                        return $"[0x{AbsoluteAddress:X}]";
                    }
                }
                else if (IsRegister)
                {
                    return Register;
                }
                else
                {
                    return string.Empty;
                }
            }

            public static Operand Reg(string value)
            {
                return new Operand
                {
                    Register = value
                };
            }

            public static Operand Imm(string value)
            {
                return new Operand
                {
                    Immediate = value
                };
            }

            public static Operand Rel(ulong value)
            {
                return new Operand
                {
                    RelativeAddress = value
                };
            }

            public static Operand Rel(Operand reg, ulong value)
            {
                return new Operand
                {
                    Register = reg.Register,
                    RelativeAddress = value
                };
            }

            public static Operand Abs(ulong value)
            {
                return new Operand
                {
                    AbsoluteAddress = value
                };
            }

            public static Operand Abs(Operand reg, ulong value)
            {
                return new Operand
                {
                    Register = reg.Register,
                    AbsoluteAddress = value
                };
            }
        }
    }
}
