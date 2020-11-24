using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using URCL.NET.Compiler;
using URCL.NET.Platform8086;
using URCL.NET.VM;

namespace URCL.NET
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = new Configuration();

            //Load the configuration inputs, either from the command line arguments or console input.
            var arg = 0;
            string configLine = null;
            Type configType = configuration.GetType();
            while (configLine != string.Empty)
            {
                Console.Write("urcl/config> ");
                configLine = args.Length > 0 ? (arg >= (args.Length - 1) ? string.Empty : args[arg]) : Console.ReadLine().Trim();
                arg++;

                if (args.Length > 0) Console.WriteLine(configLine);

                if (configLine == string.Empty) break;

                var configArgs = configLine.Split(' ');

                if (configArgs.Length == 0) break;

                bool found = false;
                foreach (var prop in configType.GetProperties())
                {
                    if (prop.CanWrite && prop.Name.ToLower() == configArgs[0].ToLower())
                    {
                        found = true;

                        if (prop.PropertyType == typeof(bool))
                        {
                            prop.SetValue(configuration, true);
                        }
                        else if (configArgs.Length < 2)
                        {
                            Console.Error.WriteLine($"Configuration \"{prop.Name}\" requires a value.");
                        }
                        else if (prop.PropertyType == typeof(int))
                        {
                            if (int.TryParse(configArgs[1], out int value))
                            {
                                prop.SetValue(configuration, value);
                            }
                            else
                            {
                                Console.Error.WriteLine($"Value \"{configArgs[1]}\" is not valid for configuration \"{prop.Name}\".");
                            }
                        }
                        else if (prop.PropertyType == typeof(long))
                        {
                            if (long.TryParse(configArgs[1], out long value))
                            {
                                prop.SetValue(configuration, value);
                            }
                            else
                            {
                                Console.Error.WriteLine($"Value \"{configArgs[1]}\" is not valid for configuration \"{prop.Name}\".");
                            }
                        }
                        else if (prop.PropertyType == typeof(ulong))
                        {
                            if (ulong.TryParse(configArgs[1], out ulong value))
                            {
                                prop.SetValue(configuration, value);
                            }
                            else
                            {
                                Console.Error.WriteLine($"Value \"{configArgs[1]}\" is not valid for configuration \"{prop.Name}\".");
                            }
                        }
                        else if (prop.PropertyType == typeof(string))
                        {
                            prop.SetValue(configuration, string.Join(" ", configArgs.Skip(1)));
                        }
                        else
                        {
                            Console.Error.WriteLine($"Configuration \"{prop.Name}\" is not supported.");
                        }
                    }
                }

                if (!found) Console.Error.WriteLine($"Configuration \"{configArgs[0]}\" is not valid.");
            }

            //Load the input file, either from the command line arguments or console input.
            Console.Write("urcl/input> ");
            string file = args.Length > 0 ? args[^1] : Console.ReadLine();
            if (args.Length > 0) Console.WriteLine(file);

            //Process the file based on its type.
            if (File.Exists(file))
            {
                if (file.EndsWith(".urcl") || file.EndsWith(".txt")) //Compile the URCL and either output it as a .NET application or emulate it.
                {
                    Console.WriteLine($"Compiling URCL source \"{file}\".");
                    IEnumerable<UrclInstruction> instructions;

                    try
                    {
                        var emitter = new ILEmitter();

                        var output = configuration.Output ?? "Program";
                        instructions = Parser.Parse(File.ReadAllLines(file));

                        if (!configuration.Emulate)
                        {
                            emitter.Emit(output, instructions);
                            File.WriteAllText($"{output}.runtimeconfig.json", "{\"runtimeOptions\":{\"tfm\":\"netcoreapp3.1\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\":\"3.1.0\"}}}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Translation failed: {ex.Message}");
                        Environment.Exit(3);
                        return;
                    }

                    if (configuration.Emulate)
                    {
                        var machine = new UrclMachine(1, configuration.Registers, configuration.AvailableMemory, configuration.AvailableROM, configuration.ExecuteOnROM, configuration.WordBitMask, new ConsoleIO());

                        if (configuration.ExecuteOnROM)
                        {
                            machine.LoadROM(0, instructions);
                        }
                        else
                        {
                            machine.LoadRAM(0, instructions);
                        }

                        Console.WriteLine();

                        var start = Environment.TickCount64;

                        while (!machine.Halted)
                        {
                            try
                            {
                                var brk = machine.Clock();

                                if (brk || configuration.StepThrough)
                                {
                                    if (!configuration.StepThrough)
                                    {
                                        Console.WriteLine("Breakpoint hit! System suspended.");
                                        Console.WriteLine("Dumping machine state...");
                                    }

                                    RenderMachineState(machine, configuration.StepThrough);

                                    Console.WriteLine("Press any key to continue...");
                                    Console.ReadKey(true);
                                }
                            }
                            catch (UrclMachine.InvalidOperationException ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Fault! {ex.Message}");
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                break;
                            }
                        }

                        Console.WriteLine($"System halted! Execution finished in {Environment.TickCount64 - start}ms ({machine.Ticks} ticks).");
                        Console.WriteLine("Dumping final machine state...");

                        RenderMachineState(machine, true);
                    }
                }
                else if (file.EndsWith(".sped")) //Compile the SpeedAsm source and output it based on configuration.
                {
                    Console.WriteLine($"Processing SpeedAsm source \"{file}\"");

                    try
                    {
                        var src = File.ReadAllLines(file);
                        var emit = new PlatformSpeedAsm.SpeedAsm();

                        if (!SpeedAsm.Compiler.Build(emit, src, out int line, out string error))
                        {
                            Console.WriteLine($"Compile Error: Ln {line + 1}, {error}");
                            Environment.Exit(8);
                        }

                        Console.WriteLine(emit);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Fatal error occured: {ex}");
                        Environment.Exit(9);
                    }
                }
                else if (file.EndsWith(".l") || file.EndsWith(".c") || file.EndsWith(".h")) //Compile the LuC source and output it based on configuration.
                {
                    Console.WriteLine($"Processing LuC source \"{file}\"");

                    try
                    {
                        var src = File.ReadAllText(file);

                        try
                        {
                            var compiler = new LuC.Compiler();
                            var ast = LuC.Parser.Parse(compiler, src).ToArray();
                            compiler.Compile(ast);
                            LuC.Standard.AddStandardDefaults(compiler);
                            var emit = new PlatformLuC.Emitters.Urcl(compiler);
                            compiler.Emit(emit, "Program.Main");
                            IGenerator generator = new PassthroughGenerator();
                            generator.Generate(Console.WriteLine, new UrclOptimizer
                            {
                                CullRedundantStackOps = true,
                                CullCreateLabel = true,
                                CullPragmas = true
                            }.Optimize(emit.Result.ToArray()));
                        }
                        catch (LuC.SourceError ex)
                        {
                            ex.GetLineAndColumn(src, out int line, out int column);
                            Console.WriteLine($"Compile Error: Ln {line}, Col {column}, Len {ex.Length}: {ex.Message}");

                            var findStart = Math.Max(0, ex.Start - 5);
                            var findLength = Math.Min(src.Length, findStart + ex.Length + 10) - findStart;
                            Console.Write(src[findStart..ex.Start]);
                            var orig = Console.ForegroundColor;
                            var origBack = Console.BackgroundColor;
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.BackgroundColor = ConsoleColor.DarkRed;
                            Console.Write(src.Substring(ex.Start, ex.Length));
                            Console.ForegroundColor = orig;
                            Console.BackgroundColor = origBack;
                            Console.WriteLine(src.Substring(ex.Start + ex.Length, findLength - (ex.Length + (ex.Start - findStart))));
                            Environment.Exit(6);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Fatal error occured: {ex}");
                        Environment.Exit(7);
                    }
                }
                else if (file.EndsWith(".exe") || file.EndsWith(".dll")) //Load the .NET assembly and compile it to URCL.
                {
                    Console.WriteLine($"Loading .NET assembly \"{file}\".");

                    ModuleDefinition assembly = null;

                    try
                    {
                        assembly = ModuleDefinition.ReadModule(file);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Assembly load failed. \"{ex.Message}\"");
                        Console.WriteLine("Full Exception:");
                        Console.WriteLine(ex);
                        Environment.Exit(2);
                    }

                    if (assembly != null)
                    {
                        var compiler = new AssemblyCompiler(configuration, assembly);

                        compiler.PreCompile();

                        compiler.Resolve();

                        var output = new OutputGenerator();

                        output.EntryPoint(configuration, compiler.EntryPoint);

                        output.AddBlock(compiler);

                        output.Output(Console.Out);
                    }
                }
                else
                {
                    Console.WriteLine($"File \"{file}\" is not supported.");
                    Environment.Exit(5);
                }
            }
            else
            {
                Console.WriteLine($"File \"{file}\" was not found.");
                Environment.Exit(1);
            }

            Console.WriteLine("Finished.");
        }

        private static void RenderMachineState(UrclMachine machine, bool allCores)
        {
            Console.WriteLine($"Cores: {machine.Cores.LongLength}, Word Mask: 0x{machine.BitMask:X}, RAM: {machine.RAM.LongLength} words, ROM: {machine.ROM.LongLength} words");
            Console.WriteLine($"Current Core: {machine.CurrentCore}");

            if (allCores)
            {
                ulong i = 0;
                foreach (var core in machine.Cores)
                {
                    RenderCoreState(i, core);
                    i++;
                }
            }
            else
            {
                RenderCoreState(machine.CurrentCore, machine.Cores[machine.CurrentCore]);
            }
        }

        private static void RenderCoreState(ulong index, UrclMachine.Core core)
        {
            Console.WriteLine($"Core {index}:");
            Console.WriteLine($"\tInstruction Pointer: 0x{core.InstructionPointer.ToString("X").PadLeft(8, '0')}, Last Value: {core.Flags:X}, Halted: {(core.Halted ? "Yes" : "No")}");

            Console.WriteLine("\tRegisters:");
            for (ulong i = 0; i < (ulong)core.Registers.LongLength; i++)
            {
                Console.WriteLine($"\t\tR{i + 1}: 0x{core.Registers[i].ToString("X").PadLeft(8, '0')}");
            }

            var stack = core.CallStack.ToArray();

            Console.WriteLine("\tCall Stack:");
            for (ulong i = 0; i < (ulong)stack.LongLength; i++)
            {
                Console.WriteLine($"\t\t[{i}]: 0x{stack[i].ToString("X").PadLeft(8, '0')}");
            }

            stack = core.ValueStack.ToArray();

            Console.WriteLine("\tValue Stack:");
            for (ulong i = 0; i < (ulong)stack.LongLength; i++)
            {
                Console.WriteLine($"\t\t[{i}]: 0x{stack[i].ToString("X").PadLeft(8, '0')}");
            }
        }
    }
}
