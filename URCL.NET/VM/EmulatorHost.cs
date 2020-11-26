using System;
using System.Collections.Generic;
using System.Linq;

namespace URCL.NET.VM
{
    public static class EmulatorHost
    {
        public static void Emulator(Configuration configuration, IEnumerable<UrclInstruction> instructions, Action<string> output, Action wait, bool allowConsole)
        {
            var wordSize = configuration.WordSize;
            instructions = instructions.Select(inst => 
            {
                if (inst.Operation == Operation.BITS)
                {
                    if (inst.A > 0 && inst.A <= configuration.WordSize)
                    {
                        wordSize = (ushort)inst.A;
                    }
                }

                return inst;
            }).Where(inst => inst.Operation != Operation.BITS).Append(new UrclInstruction(Operation.HLT)).ToArray();

            var machine = new UrclMachine(1, configuration.Registers, configuration.MaxStack, configuration.AvailableMemory, configuration.AvailableROM, configuration.ExecuteOnROM, Configuration.GetBitMask(wordSize), allowConsole ? new ConsoleIO() : null);

            if (configuration.ExecuteOnROM)
            {
                machine.LoadROM(0, instructions);
            }
            else
            {
                machine.LoadRAM(0, instructions);
            }

            var start = Environment.TickCount64;
            var timeLimit = (long)configuration.MaxTime + start;
            var fault = false;

            while (!machine.Halted && (configuration.StepThrough || timeLimit - Environment.TickCount64 > 0))
            {
                try
                {
                    var brk = machine.Clock();

                    if ((brk && !configuration.DisableBreak) || configuration.StepThrough)
                    {
                        if (!configuration.StepThrough)
                        {
                            output("Breakpoint hit! System suspended.");
                            output("Dumping machine state...");
                        }

                        RenderMachineState(machine, configuration.StepThrough, output);

                        output("Press any key to continue...");
                        wait?.Invoke();
                    }
                }
                catch (UrclMachine.InvalidOperationException ex)
                {
                    output($"Fault! {ex.Message}");
                    fault = true;
                    break;
                }
            }

            if (!machine.Halted && !fault) output("Maximum time for execution was exceeded!");

            output($"System halted. Execution finished in {Environment.TickCount64 - start}ms ({machine.Ticks} ticks).");
            output("Dumping final machine state...");

            RenderMachineState(machine, true, output);
        }

        private static void RenderMachineState(UrclMachine machine, bool allCores, Action<string> output)
        {
            output($"Cores: {machine.Cores.LongLength}, Word Mask: 0x{machine.BitMask:X}, RAM: {machine.RAM.LongLength} words, ROM: {machine.ROM.LongLength} words");
            output($"Current Core: {machine.CurrentCore}");

            if (allCores)
            {
                ulong i = 0;
                foreach (var core in machine.Cores)
                {
                    RenderCoreState(i, core, output);
                    i++;
                }
            }
            else
            {
                RenderCoreState(machine.CurrentCore, machine.Cores[machine.CurrentCore], output);
            }
        }

        private static void RenderCoreState(ulong index, UrclMachine.Core core, Action<string> output)
        {
            output($"Core {index}:");
            output($"\tInstruction Pointer: 0x{core.InstructionPointer.ToString("X").PadLeft(8, '0')}, Last Value: {(uint)core.Flags:X}, Halted: {(core.Halted ? "Yes" : "No")}");

            output("\tRegisters:");
            for (ulong i = 0; i < (ulong)core.Registers.LongLength; i++)
            {
                output($"\t\tR{i + 1}: 0x{core.Registers[i].ToString("X").PadLeft(8, '0')}");
            }

            var stack = core.CallStack.ToArray();

            output("\tCall Stack:");
            for (ulong i = 0; i < (ulong)stack.LongLength; i++)
            {
                output($"\t\t[{i}]: 0x{stack[i].ToString("X").PadLeft(8, '0')}");
            }

            stack = core.ValueStack.ToArray();

            output("\tValue Stack:");
            for (ulong i = 0; i < (ulong)stack.LongLength; i++)
            {
                output($"\t\t[{i}]: 0x{stack[i].ToString("X").PadLeft(8, '0')}");
            }
        }
    }
}
