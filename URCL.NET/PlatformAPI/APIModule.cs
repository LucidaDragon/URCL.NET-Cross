using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using URCL.NET.Compiler;
using URCL.NET.VM;

namespace URCL.NET.PlatformAPI
{
    public class APIModule
    {
        public static void HandleConfiguration(Configuration configuration)
        {
            if (configuration.ApiPort > 0)
            {
                var tcp = new TcpListener(new IPEndPoint(IPAddress.Loopback, configuration.ApiPort));

                tcp.Start();

                while (true)
                {
                    try
                    {
                        using var connection = tcp.AcceptTcpClient();
                        using var reader = new StreamReader(connection.GetStream());
                        using var writer = new StreamWriter(connection.GetStream());

                        var builder = new ConfigurationBuilder();
                        var configCount = ushort.Parse(reader.ReadLine());
                        for (ushort i = 0; i < configCount; i++)
                        {
                            builder.Configure(reader.ReadLine(), writer.WriteLine);
                        }

                        writer.WriteLine(string.Empty);

                        var lineCount = ulong.Parse(reader.ReadLine());

                        if (lineCount == 0) continue;

                        var buffer = new List<string>();

                        for (ulong i = 0; i < lineCount; i++)
                        {
                            buffer.Add(reader.ReadLine());
                        }

                        IEnumerable<UrclInstruction> instructions;

                        try
                        {
                            instructions = Parser.Parse(buffer);
                        }
                        catch (ParserError ex)
                        {
                            writer.WriteLine(ex.Message);
                            continue;
                        }

                        EmulatorHost.Emulator(configuration, instructions, writer.WriteLine, () => { });

                        writer.WriteLine(string.Empty);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"API: {ex.Message}");
                    }
                }
            }
        }
    }
}
