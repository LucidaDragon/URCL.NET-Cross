using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UrclBot
{
    public class Bot : IDisposable
    {
        private readonly UrclInterface Urcl;
        private readonly DiscordSocketClient Client;
        private readonly string Token;
        private readonly ConcurrentQueue<Tuple<SocketMessage, Attachment>> Jobs = new ConcurrentQueue<Tuple<SocketMessage, Attachment>>();
        private readonly Thread Worker;
        private readonly AutoResetEvent Sleep = new AutoResetEvent(false);
        private bool Quit = false;

        public Bot(UrclInterface urcl, Action<string> output, string token)
        {
            Urcl = urcl;
            Token = token;

            Worker = new Thread(async () =>
            {
                while (true)
                {
                    Sleep.WaitOne();

                    if (Quit) break;

                    while (Jobs.TryDequeue(out Tuple<SocketMessage, Attachment> job))
                    {
                        var m = job.Item1;
                        var attach = job.Item2;

                        try
                        {
                            using var fetch = new WebClient();
                            var content = await fetch.DownloadStringTaskAsync(attach.Url);

                            var buffer = new List<string>();

                            await Urcl.SubmitJob(content, buffer.Add);

                            Reply(m, $"Result of \"{attach.Filename}\":{Environment.NewLine}{string.Join(Environment.NewLine, buffer)}");
                        }
                        catch (Exception ex)
                        {
                            Reply(m, $"API Error: {ex.Message}");
                        }
                    }
                }
            });
            Worker.Start();

            Client = new DiscordSocketClient();

            Client.Log += (m) =>
            {
                output($"[\"{m.Severity}\"] {m.Message} {m.Exception}");

                return Task.CompletedTask;
            };

            Client.MessageReceived += (m) =>
            {
                foreach (var user in m.MentionedUsers)
                {
                    if (user.IsBot && user.Id == Client.CurrentUser.Id)
                    {
                        foreach (var attach in m.Attachments)
                        {
                            if (attach.Filename.ToLower().EndsWith("urcl"))
                            {
                                if (attach.Size <= ushort.MaxValue)
                                {
                                    Reply(m, $"\"{attach.Filename}\" is now in queue.");

                                    Jobs.Enqueue(new Tuple<SocketMessage, Attachment>(m, attach));
                                    Sleep.Set();
                                }
                                else
                                {
                                    Reply(m, $"Attached file \"{attach.Filename}\" is too large. (Must be {ushort.MaxValue} bytes or less)");
                                }
                            }
                        }
                    }
                }

                return Task.CompletedTask;
            };

            Client.Disconnected += (e) =>
            {
                new Thread(async () =>
                {
                    await Client.LoginAsync(TokenType.Bot, Token);
                    await Client.StartAsync();
                }).Start();

                return Task.CompletedTask;
            };
        }

        public async void Start()
        {
            await Client.LoginAsync(TokenType.Bot, Token);
            await Client.StartAsync();
        }

        public void Dispose()
        {
            Client.Dispose();
            Urcl.Dispose();
            Quit = true;
            Sleep.Set();
        }

        private async void Reply(SocketMessage source, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            if (message.Length < 2000)
            {
                await source.Channel.SendMessageAsync(message);
            }
            else
            {
                await source.Channel.SendFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(message)), "Process_Result", ".");
            }
        }
    }
}
