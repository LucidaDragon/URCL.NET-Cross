using System;
using System.Collections.Generic;
using System.IO;
using URCL.NET.Compiler;
using URCL.NET.VM;

namespace URCL.NET
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //Load the configuration inputs, either from the command line arguments or console input.
                var arg = 0;
                string configLine = null;
                var builder = new ConfigurationBuilder();
                while (configLine != string.Empty)
                {
                    Console.Write("urcl/config> ");
                    configLine = args.Length > 0 ? (arg >= (args.Length - 1) ? string.Empty : args[arg]) : Console.ReadLine().Trim();
                    arg++;

                    if (args.Length > 0) Console.WriteLine(configLine);

                    if (configLine == string.Empty) break;

                    builder.Configure(configLine, Console.Error.WriteLine);
                }

                var configuration = builder.Configuration;

                if (string.IsNullOrEmpty(configuration.Output)) configuration.Output = "output.urcl";

                //Load compiler modules.
                var moduleLoader = new ModuleLoader();
                moduleLoader.AddFileType("urcl", "URCL");
                moduleLoader.Load(configuration);
                moduleLoader.RunConfigurations(configuration);

                //Load the input file, either from the command line arguments or console input.
                Console.Write("urcl/input> ");
                string file = args.Length > 0 ? args[^1] : Console.ReadLine();
                if (args.Length > 0) Console.WriteLine(file);

                //Process the file based on its type.
                if (File.Exists(file))
                {
                    var inExt = Path.GetExtension(file).ToLower().TrimStart('.');
                    var outExt = Path.GetExtension(configuration.Output).ToLower().TrimStart('.');

                    Console.WriteLine($"Processing {moduleLoader.GetFileType(inExt)} source \"{file}\" to {moduleLoader.GetFileType(outExt)} \"{configuration.Output}\".");

                    if (inExt == "urcl")
                    {
                        var instructions = Parser.Parse(File.ReadAllLines(file));

                        if (configuration.Emulate)
                        {
                            EmulatorHost.Emulator(configuration, instructions, Console.WriteLine, () => { Console.ReadKey(true); }, true);
                        }
                        else
                        {
                            using var stream = new FileStream(configuration.Output, FileMode.Create, FileAccess.ReadWrite);

                            if (!moduleLoader.ExecuteEmitter(outExt, stream.WriteByte, instructions))
                            {
                                Console.WriteLine($"File \"{file}\" to output \"{configuration.Output}\" is not supported.");
                                Environment.Exit(2);
                                return;
                            }
                        }
                    }
                    else
                    {
                        using var input = new FileStream(file, FileMode.Open, FileAccess.Read);

                        if (configuration.Emulate)
                        {
                            var lines = new List<string>();

                            if (!moduleLoader.ExecuteFileHandler(inExt, input, lines.Add, Console.WriteLine))
                            {
                                Console.WriteLine($"File \"{file}\" is not supported.");
                                Environment.Exit(2);
                                return;
                            }

                            EmulatorHost.Emulator(configuration, Parser.Parse(lines), Console.WriteLine, () => { Console.ReadKey(true); }, true);
                        }
                        else
                        {
                            using var writer = new StreamWriter(new FileStream(configuration.Output, FileMode.Create, FileAccess.ReadWrite));

                            if (!moduleLoader.ExecuteFileHandler(inExt, input, writer.WriteLine, Console.WriteLine))
                            {
                                Console.WriteLine($"File \"{file}\" is not supported.");
                                Environment.Exit(2);
                                return;
                            }
                        }
                    }

                    return;
                }
                else
                {
                    Console.WriteLine($"File \"{file}\" was not found.");
                    Environment.Exit(1);
                }

                Console.WriteLine("Finished.");
            }
            catch (ParserError ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal Error: {ex}");
            }
        }
    }
}
