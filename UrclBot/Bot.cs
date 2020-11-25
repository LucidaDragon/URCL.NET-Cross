using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace UrclBot
{
    public class Bot : IDisposable
    {
        private readonly UrclInterface Urcl;
        private readonly DiscordSocketClient Client;
        private readonly string Token;
        private readonly ConcurrentQueue<BotTask> Jobs = new ConcurrentQueue<BotTask>();
        private readonly Thread Worker;
        private readonly AutoResetEvent WaitForConnect = new AutoResetEvent(false);
        private readonly AutoResetEvent Sleep = new AutoResetEvent(false);
        private bool Quit = false;

        public Bot(UrclInterface urcl, Action<string> output, string token)
        {
            Urcl = urcl;
            Token = token;

            Worker = new Thread(async () =>
            {
                WaitForConnect.WaitOne();

                while (true)
                {
                    output($"{DateTime.Now} Idle");
                    await Client.SetStatusAsync(UserStatus.Idle);

                    Sleep.WaitOne();

                    if (Quit) break;

                    while (Jobs.TryDequeue(out BotTask job))
                    {
                        output($"{DateTime.Now} Active");
                        await Client.SetStatusAsync(UserStatus.Online);

                        try
                        {
                            var buffer = new List<string>();

                            await Urcl.SubmitJob(await job.GetContent(), buffer.Add);

                            Reply(job.Source, $"Result of \"{job.Name}\":{Environment.NewLine}{string.Join(Environment.NewLine, buffer)}");
                        }
                        catch (Exception ex)
                        {
                            Reply(job.Source, $"API Error: {ex.Message}");
                        }
                    }
                }
            });

            Client = new DiscordSocketClient();

            Client.Log += (m) =>
            {
                output($"[\"{m.Severity}\"] {m.Message} {m.Exception}");

                return Task.CompletedTask;
            };

            Client.Ready += () =>
            {
                WaitForConnect.Set();
                return Task.CompletedTask;
            };

            Client.MessageReceived += (m) =>
            {
                foreach (var user in m.MentionedUsers)
                {
                    if (user.IsBot && user.Id == Client.CurrentUser.Id)
                    {
                        var foundFile = false;

                        foreach (var attach in m.Attachments)
                        {
                            if (attach.Filename.ToLower().EndsWith("urcl"))
                            {
                                foundFile = true;

                                if (attach.Size <= ushort.MaxValue)
                                {
                                    Reply(m, $"\"{attach.Filename}\" is now in queue.");

                                    Jobs.Enqueue(new BotTask(m, attach));
                                    Sleep.Set();
                                }
                                else
                                {
                                    Reply(m, $"Attached file \"{attach.Filename}\" is too large. (Must be {ushort.MaxValue} bytes or less)");
                                }
                            }
                        }

                        if (!foundFile)
                        {
                            var match = Regex.Match(m.Content, @"(```((.|\n)*)```)");

                            if (match.Success)
                            {
                                Jobs.Enqueue(new BotTask(m, match.Groups[2].Value));
                                Sleep.Set();
                            }
                            else
                            {
                                Reply(m, "Code block was not specified.");
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
            Worker.Start();
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
                await source.Channel.SendFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(message)), "message.txt", "Message was too large to display...");
            }
        }
    }
}
