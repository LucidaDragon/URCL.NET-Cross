using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace URCL.NET.VM
{
    public class UrclMachine
    {
        public Dictionary<Label, ulong> RamLabels { get; set; } = new Dictionary<Label, ulong>();
        public Dictionary<Label, ulong> RomLabels { get; set; } = new Dictionary<Label, ulong>();

        public bool Halted
        {
            get
            {
                for (ulong i = 0; i < (ulong)Cores.LongLength; i++)
                {
                    if (!Cores[i].Halted) return false;
                }

                return true;
            }
        }

        public object[] RAM { get; set; }
        public object[] ROM { get; set; }
        public Core[] Cores { get; set; }
        public IO IoBus { get; set; }
        public ulong BitMask { get; set; }
        public ulong CurrentCore { get; set; } = 0;
        public ulong Ticks { get; set; } = 0;

        public UrclMachine(ulong cores, ulong registers, ulong maxStack, ulong ram, ulong rom, bool coresExecOnROM = false, ulong bitmask = uint.MaxValue, IO ioBus = null)
        {
            Cores = new Core[cores];
            for (ulong i = 0; i < cores; i++)
            {
                Cores[i] = new Core(this, coresExecOnROM, registers, maxStack);
            }

            RAM = new object[ram];
            ROM = new object[rom];

            BitMask = bitmask;

            IoBus = ioBus is null ? new DefaultIO() : ioBus;
        }

        public ulong LoadRAM(ulong address, IEnumerable data)
        {
            return LoadMemory(RAM, RamLabels, address, data);
        }

        public ulong LoadROM(ulong address, IEnumerable data)
        {
            return LoadMemory(ROM, RomLabels, address, data);
        }

        private ulong LoadMemory(object[] memory, Dictionary<Label, ulong> labels, ulong address, IEnumerable data)
        {
            ulong start = address;

            foreach (var item in data)
            {
                if (item is Label l)
                {
                    labels[l] = address;
                }
                else if (item is UrclInstruction inst && ((inst.Operation == Operation.COMPILER_MARKLABEL && inst.AType == OperandType.Label) || ((inst.Operation == Operation.DW || inst.Operation == Operation.DD || inst.Operation == Operation.DQ) && inst.AType == OperandType.Immediate)))
                {
                    switch (inst.Operation)
                    {
                        case Operation.COMPILER_MARKLABEL:
                            labels[inst.ALabel] = address;
                            break;
                        case Operation.DW:
                            memory[address] = inst.A & BitMask;
                            address++;
                            break;
                        case Operation.DD:
                            {
                                SplitBits(inst.A, BitMask, out ulong lower, out ulong upper, out _, out _);
                                memory[address] = lower;
                                memory[address + 1] = upper;
                                address += 2;
                            }
                            break;
                        case Operation.DQ:
                            {
                                SplitBits(inst.A, BitMask, out ulong lower, out ulong upper, out ulong lowerMask, out ulong upperMask);
                                SplitBits(lower, lowerMask, out ulong lowerlower, out ulong upperlower, out _, out _);
                                SplitBits(upper, upperMask, out ulong lowerupper, out ulong upperupper, out _, out _);
                                memory[address] = lowerlower;
                                memory[address + 1] = upperlower;
                                memory[address + 2] = lowerupper;
                                memory[address + 3] = upperupper;
                                address += 4;
                            }
                            break;
                    }
                }
                else
                {
                    memory[address] = item;
                    address++;
                }
            }

            var length = address - start;

            for (ulong i = 0; i < length; i++)
            {
                if (memory[i] is UrclInstruction inst)
                {
                    memory[i] = Decode(inst);
                }
            }

            return address;
        }

        private ResolvedInstruction Decode(UrclInstruction inst)
        {
            var argCount = 0;

            if (inst.AType != OperandType.None) argCount++;
            if (inst.BType != OperandType.None) argCount++;
            if (inst.CType != OperandType.None) argCount++;

            var args = new object[argCount];

            if (argCount >= 1)
            {
                args[0] = inst.AType switch
                {
                    OperandType.Register => new Register(inst.A),
                    OperandType.Immediate => inst.A,
                    _ => ResolveLabel(inst.ALabel),
                };
            }

            if (argCount >= 2)
            {
                args[1] = inst.BType switch
                {
                    OperandType.Register => new Register(inst.B),
                    OperandType.Immediate => inst.B,
                    _ => ResolveLabel(inst.BLabel),
                };
            }

            if (argCount >= 3)
            {
                args[2] = inst.CType switch
                {
                    OperandType.Register => new Register(inst.C),
                    OperandType.Immediate => inst.C,
                    _ => ResolveLabel(inst.CLabel),
                };
            }

            return new ResolvedInstruction(inst.Operation, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong ResolveLabel(Label label)
        {
            if (label != null && RomLabels.TryGetValue(label, out ulong v))
            {
                return v;
            }
            else if (label != null && RamLabels.TryGetValue(label, out v))
            {
                return v;
            }
            else
            {
                throw new InvalidOperationException(this, InvalidOperationException.UnresolvedLabel);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Clock()
        {
            Ticks++;

            for (; CurrentCore < (ulong)Cores.LongLength; CurrentCore++)
            {
                var core = Cores[CurrentCore];

                if (!core.Halted)
                {
                    if (!core.Clock() && !core.Halted)
                    {
                        return true;
                    }
                }
            }

            CurrentCore = 0;

            return false;
        }

        private static void SplitBits(ulong value, ulong bitmask, out ulong lower, out ulong upper, out ulong lowerMask, out ulong upperMask)
        {
            var width = GetBits(bitmask);
            var lowerWidth = width / 2;
            upperMask = bitmask >> lowerWidth;
            lowerMask = (upperMask << lowerWidth) ^ bitmask;
            upper = (value >> lowerWidth) & upperMask;
            lower = value & lowerMask;
        }

        private static int GetBits(ulong mask)
        {
            var bits = 0;

            while (mask != 0)
            {
                mask >>= 1;
                bits++;
            }

            return bits;
        }

        public class Core
        {
            public UrclMachine Host;
            public Stack<ulong> ValueStack = new Stack<ulong>();
            public Stack<ulong> CallStack = new Stack<ulong>();
            public ulong MaxStack;
            public ulong InstructionPointer = 0;
            public ulong Ticks = 0;
            public bool Halted = false;
            public bool ExecuteFromROM = false;
            public ulong[] Registers;
            public ulong Flags = 0;

            public Core(UrclMachine host, bool executeFromROM, ulong registers, ulong maxStack)
            {
                Host = host;
                ExecuteFromROM = executeFromROM;
                Registers = new ulong[registers];
                MaxStack = maxStack;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Clock()
            {
                Ticks++;

                return Execute(Fetch());
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool Execute(ResolvedInstruction inst)
            {
                switch (inst.Operation)
                {
                    case Operation.NOP:
                        break;
                    case Operation.BRK:
                        return false;
                    case Operation.HLT:
                        Halted = true;
                        return false;
                    case Operation.ADD:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B) && inst.Exists(Operand.C))
                        {
                            SetRegister(inst[Operand.A], Flags = (ResolveValue(inst[Operand.B]) + ResolveValue(inst[Operand.C])));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.INC:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B))
                        {
                            SetRegister(inst[Operand.A], Flags = (ResolveValue(inst[Operand.B]) + 1));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.SUB:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B) && inst.Exists(Operand.C))
                        {
                            SetRegister(inst[Operand.A], Flags = (ResolveValue(inst[Operand.B]) + (~ResolveValue(inst[Operand.C]) + 1)));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.DEC:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B))
                        {
                            SetRegister(inst[Operand.A], Flags = (ResolveValue(inst[Operand.B]) - 1));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.MLT:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B) && inst.Exists(Operand.C))
                        {
                            SetRegister(inst[Operand.A], Flags = (ResolveValue(inst[Operand.B]) * ResolveValue(inst[Operand.C])));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.DIV:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B) && inst.Exists(Operand.C))
                        {
                            SetRegister(inst[Operand.A], Flags = (ResolveValue(inst[Operand.B]) / ResolveValue(inst[Operand.C])));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.MOD:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B) && inst.Exists(Operand.C))
                        {
                            SetRegister(inst[Operand.A], Flags = (ResolveValue(inst[Operand.B]) % ResolveValue(inst[Operand.C])));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.CMP:
                        if (inst.Exists(Operand.A) && inst.Exists(Operand.B))
                        {
                            Flags = (ResolveValue(inst[Operand.A]) - ResolveValue(inst[Operand.B]));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.AND:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B) && inst.Exists(Operand.C))
                        {
                            SetRegister(inst[Operand.A], Flags = (ResolveValue(inst[Operand.B]) & ResolveValue(inst[Operand.C])));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.OR:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B) && inst.Exists(Operand.C))
                        {
                            SetRegister(inst[Operand.A], Flags = (ResolveValue(inst[Operand.B]) | ResolveValue(inst[Operand.C])));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.XOR:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B) && inst.Exists(Operand.C))
                        {
                            SetRegister(inst[Operand.A], Flags = (ResolveValue(inst[Operand.B]) ^ ResolveValue(inst[Operand.C])));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.NAND:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B) && inst.Exists(Operand.C))
                        {
                            SetRegister(inst[Operand.A], Flags = (~(ResolveValue(inst[Operand.B]) & ResolveValue(inst[Operand.C]))));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.NOR:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B) && inst.Exists(Operand.C))
                        {
                            SetRegister(inst[Operand.A], Flags = (~(ResolveValue(inst[Operand.B]) | ResolveValue(inst[Operand.C]))));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.XNOR:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B) && inst.Exists(Operand.C))
                        {
                            SetRegister(inst[Operand.A], Flags = (~(ResolveValue(inst[Operand.B]) ^ ResolveValue(inst[Operand.C]))));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.NOT:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B))
                        {
                            SetRegister(inst[Operand.A], Flags = (~ResolveValue(inst[Operand.B])));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.LSH:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B))
                        {
                            SetRegister(inst[Operand.A], Flags = (ResolveValue(inst[Operand.B]) << 1));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.BSL:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B) && inst.Exists(Operand.C))
                        {
                            SetRegister(inst[Operand.A], Flags = (ResolveValue(inst[Operand.B]) << (int)(ResolveValue(inst[Operand.C]) & 0x7F)));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.RSH:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B))
                        {
                            SetRegister(inst[Operand.A], Flags = (ResolveValue(inst[Operand.B]) >> 1));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.BSR:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B) && inst.Exists(Operand.C))
                        {
                            SetRegister(inst[Operand.A], Flags = (ResolveValue(inst[Operand.B]) >> (int)(ResolveValue(inst[Operand.C]) & 0x7F)));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.MOV:
                    case Operation.IMM:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B))
                        {
                            SetRegister(inst[Operand.A], ResolveValue(inst[Operand.B]));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.LOAD:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B))
                        {
                            var address = ResolveValue(inst[Operand.B]);

                            if (address > (ulong)Host.RAM.LongLength)
                            {
                                throw new InvalidOperationException(this, InvalidOperationException.NotEnoughMemory);
                            }

                            SetRegister(inst[Operand.A], ResolveValue(Host.RAM[address]));
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.STORE:
                        if (inst.Exists(Operand.A) && inst.Exists(Operand.B))
                        {
                            var address = ResolveValue(inst[Operand.A]);
                            var value = ResolveValue(inst[Operand.B]);

                            if (address > (ulong)Host.RAM.LongLength)
                            {
                                throw new InvalidOperationException(this, InvalidOperationException.NotEnoughMemory);
                            }

                            Host.RAM[address] = value;
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.IN:
                        if (inst.IsRegister(Operand.A) && inst.Exists(Operand.B))
                        {
                            SetRegister(inst[Operand.A], Host.IoBus[ResolveValue(inst[Operand.B])]);
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.OUT:
                        if (inst.Exists(Operand.A) && inst.Exists(Operand.B))
                        {
                            Host.IoBus[ResolveValue(inst[Operand.A])] = ResolveValue(inst[Operand.B]);
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.PSH:
                        if (inst.Exists(Operand.A))
                        {
                            ValueStack.Push(ResolveValue(inst[Operand.A]));
                            if ((ulong)ValueStack.Count > MaxStack) throw new InvalidOperationException(this, InvalidOperationException.StackOverflow);
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.POP:
                        if (ValueStack.Count == 0) throw new InvalidOperationException(this, InvalidOperationException.StackEmpty);
                        if (inst.IsRegister(Operand.A))
                        {
                            SetRegister(inst[Operand.A], ValueStack.Pop());
                        }
                        else if (!inst.Exists(Operand.A))
                        {
                            ValueStack.Pop();
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.BRA:
                        if (inst.Exists(Operand.A))
                        {
                            InstructionPointer = ResolveValue(inst[Operand.A]) - 1;
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.BRZ:
                        if (inst.Exists(Operand.A))
                        {
                            if ((Flags & Host.BitMask) == 0) InstructionPointer = ResolveValue(inst[Operand.A]) - 1;
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.BNZ:
                        if (inst.Exists(Operand.A))
                        {
                            if ((Flags & Host.BitMask) != 0) InstructionPointer = ResolveValue(inst[Operand.A]) - 1;
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.BRC:
                        if (inst.Exists(Operand.A))
                        {
                            if (Flags > Host.BitMask) InstructionPointer = ResolveValue(inst[Operand.A]) - 1;
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.BNC:
                        if (inst.Exists(Operand.A))
                        {
                            if (Flags <= Host.BitMask) InstructionPointer = ResolveValue(inst[Operand.A]) - 1;
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.BRP:
                        if (inst.Exists(Operand.A))
                        {
                            if ((Flags & Host.BitMask) <= (Host.BitMask >> 1)) InstructionPointer = ResolveValue(inst[Operand.A]) - 1;
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.BRN:
                        if (inst.Exists(Operand.A))
                        {
                            if ((Flags & Host.BitMask) > (Host.BitMask >> 1)) InstructionPointer = ResolveValue(inst[Operand.A]) - 1;
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.BEV:
                        if (inst.Exists(Operand.A) && inst.Exists(Operand.B))
                        {
                            if ((ResolveValue(inst[Operand.B]) & 1) == 0) InstructionPointer = ResolveValue(inst[Operand.A]) - 1;
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.BOD:
                        if (inst.Exists(Operand.A) && inst.Exists(Operand.B))
                        {
                            if ((ResolveValue(inst[Operand.B]) & 1) == 1) InstructionPointer = ResolveValue(inst[Operand.A]) - 1;
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.CAL:
                        if (inst.Exists(Operand.A))
                        {
                            CallStack.Push(InstructionPointer);
                            if ((ulong)CallStack.Count > MaxStack) throw new InvalidOperationException(this, InvalidOperationException.StackOverflow);
                            InstructionPointer = ResolveValue(inst[Operand.A]);
                        }
                        else
                        {
                            Invalid();
                        }
                        break;
                    case Operation.RET:
                        if (CallStack.Count == 0)
                        {
                            Halted = true;
                            return false;
                        }
                        else
                        {
                            InstructionPointer = CallStack.Pop();
                        }
                        break;
                    case Operation.MINRAM:
                    case Operation.BENCHMARK:
                    case Operation.COMPILER_CREATELABEL:
                    case Operation.COMPILER_MARKLABEL:
                    case Operation.COMPILER_MAXREG:
                    case Operation.COMPILER_COMMENT:
                    case Operation.COMPILER_PRAGMA:
                        break;
                    default:
                        Invalid();
                        break;
                }

                InstructionPointer++;
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void Invalid()
            {
                throw new InvalidOperationException(this, InvalidOperationException.InvalidInstruction);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ResolvedInstruction Fetch()
            {
                var source = ExecuteFromROM ? Host.ROM : Host.RAM;

                if (source.Length == 0)
                {
                    throw new InvalidOperationException(this, InvalidOperationException.NoMemory);
                }

                while (InstructionPointer >= (ulong)source.LongLength)
                {
                    InstructionPointer -= (ulong)source.LongLength;
                }

                var data = source[InstructionPointer];

                if (data is ResolvedInstruction inst)
                {
                    return inst;
                }
                else
                {
                    throw new InvalidOperationException(this, InvalidOperationException.InvalidFetchData);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ulong ResolveValue(object data)
            {
                if (data is ulong v)
                {
                    return v;
                }
                else if (data is Label l)
                {
                    return Host.ResolveLabel(l);
                }
                else if (data is Register r)
                {
                    if (r.Index == 0)
                    {
                        return 0;
                    }
                    else if (r.Index <= (ulong)Registers.LongLength)
                    {
                        return Registers[r.Index - 1];
                    }
                    else
                    {
                        throw new InvalidOperationException(this, InvalidOperationException.NotEnoughRegisters);
                    }
                }
                else
                {
                    throw new InvalidOperationException(this, InvalidOperationException.InvalidData);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetRegister(object register, ulong value)
            {
                if (register is Register reg)
                {
                    if (reg.Index == 0) return;

                    if (reg.Index > (ulong)Registers.LongLength)
                    {
                        throw new InvalidOperationException(this, InvalidOperationException.NotEnoughRegisters);
                    }

                    Registers[reg.Index - 1] = value & Host.BitMask;
                }
                else
                {
                    throw new InvalidOperationException(this, InvalidOperationException.InvalidRegister);
                }
            }
        }

        private struct ResolvedInstruction
        {
            public object this[Operand operand]
            {
                get => Values[((int)operand) - 1];
            }

            public Operation Operation;
            public object[] Values;

            public ResolvedInstruction(Operation op, object[] values)
            {
                Operation = op;
                Values = values;
            }

            public bool Exists(Operand operand)
            {
                return ((int)operand) <= Values.Length;
            }

            public bool IsRegister(Operand operand)
            {
                return Exists(operand) && Values[((int)operand) - 1] is Register;
            }

            public bool IsImmediate(Operand operand)
            {
                return Exists(operand) && !IsRegister(operand);
            }
        }

        private enum Operand
        {
            A = 1,
            B = 2,
            C = 3
        }

        private struct Register
        {
            public ulong Index;

            public Register(ulong index)
            {
                Index = index;
            }
        }

        private class DefaultIO : IO
        {
            public override ulong this[ulong port]
            {
                get
                {
                    UnsupportedPort();
                    return 0;
                }
                set
                {
                    UnsupportedPort();
                }
            }
        }

        public abstract class IO
        {
            public abstract ulong this[ulong port] { get; set; }

            public UrclMachine Host { get; set; }

            protected void UnsupportedPort()
            {
                throw new InvalidOperationException(Host, InvalidOperationException.PortNotSupported);
            }

            protected void IOException()
            {
                throw new InvalidOperationException(Host, InvalidOperationException.IOError);
            }
        }

        public class InvalidOperationException : Exception
        {
            public const string NoMemory = "Core does not have memory to operate on.";
            public const string NotEnoughMemory = "Core tried to access memory out of range.";
            public const string NotEnoughRegisters = "Core tried to access more registers than it has.";
            public const string StackOverflow = "Stack overflow occured.";
            public const string StackEmpty = "Stack underflow occured.";
            public const string InvalidData = "The type of data in memory is not supported.";
            public const string InvalidFetchData = "The data at the fetched address is not a valid instruction.";
            public const string InvalidInstruction = "The executed instruction was invalid.";
            public const string InvalidRegister = "Value is not a register.";
            public const string UnresolvedLabel = "Could not resolve label address.";
            public const string PortNotSupported = "Port number is not supported.";
            public const string IOError = "An IO operation caused an exception.";

            public UrclMachine MachineState { get; }
            public Core CoreState { get; }

            public InvalidOperationException(UrclMachine machine, string message) : base(message)
            {
                MachineState = machine;
            }

            public InvalidOperationException(Core core, string message) : base(message)
            {
                MachineState = core.Host;
                CoreState = core;
            }
        }
    }
}
