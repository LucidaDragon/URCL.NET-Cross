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
                        using var stream = connection.GetStream();
                        using var reader = new BinaryReader(stream);
                        using var writer = new BinaryWriter(stream);

                        var builder = new ConfigurationBuilder();
                        var configCount = ushort.Parse(reader.ReadString());
                        for (ushort i = 0; i < configCount; i++)
                        {
                            builder.Configure(reader.ReadString(), writer.Write);
                        }

                        writer.Write(string.Empty);

                        stream.Flush();

                        var lineCount = ulong.Parse(reader.ReadString());

                        if (lineCount == 0) continue;

                        var buffer = new List<string>();

                        for (ulong i = 0; i < lineCount; i++)
                        {
                            buffer.Add(reader.ReadString());
                        }

                        IEnumerable<UrclInstruction> instructions;

                        try
                        {
                            instructions = Parser.Parse(buffer);
                        }
                        catch (ParserError ex)
                        {
                            writer.Write(ex.Message);
                            stream.Flush();
                            continue;
                        }

                        EmulatorHost.Emulator(configuration, instructions, writer.Write, () => { }, false);

                        writer.Write(string.Empty);
                        stream.Flush();
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
