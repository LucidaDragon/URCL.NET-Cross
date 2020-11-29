using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UrclBot
{
    public class UrclInterface : IDisposable
    {
        public List<string> Configuration { get; set; } = new List<string>();

        private readonly string Path;
        private readonly ushort Port;

        private Process Process;

        public UrclInterface(string path, ushort port)
        {
            Path = path;
            Port = port;
        }

        public async Task SubmitJob(string job, string lang, string outputType, string tier, Action<string> output)
        {
            if (Process is null || Process.HasExited)
            {
                Process = Process.Start(Path, $"\"ApiPort {Port}\" \"\"");
            }

            using var client = new TcpClient();

            var lines = job.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim()).ToArray();

            await client.ConnectAsync(IPAddress.Loopback, Port);

            using var stream = client.GetStream();
            using var writer = new BinaryWriter(stream);
            using var reader = new BinaryReader(stream);

            writer.Write(Configuration.Count.ToString());

            foreach (var line in Configuration)
            {
                writer.Write(line);
            }

            await stream.FlushAsync();

            var response = reader.ReadString();

            if (response.Length > 0)
            {
                while (response.Length > 0)
                {
                    output(response);
                    response = reader.ReadString();
                }

                return;
            }

            writer.Write(lang);
            writer.Write(outputType);
            writer.Write(tier);

            writer.Write(lines.Length.ToString());

            foreach (var line in lines)
            {
                writer.Write(line);
            }

            await stream.FlushAsync();

            response = reader.ReadString();

            while (response.Length > 0)
            {
                output(response);
                response = reader.ReadString();
            }
        }

        public void Dispose()
        {
            Process.Kill();
            Process.Close();
        }
    }
}
