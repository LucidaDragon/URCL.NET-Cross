using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace UrclBot
{
    public class Bot : IDisposable
    {
        public ulong? Owner { get; }
        public ulong? Self { get; private set; }

        private readonly UrclInterface Urcl;
        private readonly DiscordSocketClient Client;
        private readonly string Token;
        private readonly ConcurrentQueue<BotTask> Jobs = new ConcurrentQueue<BotTask>();
        private readonly Thread Worker;
        private readonly AutoResetEvent WaitForConnect = new AutoResetEvent(false);
        private readonly AutoResetEvent Sleep = new AutoResetEvent(false);
        private bool Quit = false;

        public Bot(UrclInterface urcl, Action<string> output, string token, ulong? owner = null)
        {
            Urcl = urcl;
            Token = token;
            Owner = owner;

            Worker = new Thread(async () =>
            {
                WaitForConnect.WaitOne();

                while (true)
                {
                    output($"{DateTime.Now} Idle");
                    await Client.SetGameAsync("Conway's Game of Life.", null, ActivityType.Playing);
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

                            await Urcl.SubmitJob(await job.GetContent(), job.Language, job.OutputType, job.Tier, buffer.Add);

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
                output($"{DateTime.Now} {m.Message} {m.Exception}");

                return Task.CompletedTask;
            };

            Client.Ready += () =>
            {
                WaitForConnect.Set();
                return Task.CompletedTask;
            };

            Client.MessageReceived += (m) =>
            {
                if (!Self.HasValue && Client.CurrentUser != null) Self = Client.CurrentUser.Id;

                if (Self.HasValue)
                {
                    foreach (var user in m.MentionedUsers)
                    {
                        if (user.IsBot && user.Id == Self.Value)
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
                                var match = Regex.Match(m.Content, @"([\w]+)?\s*([\w]+)?\s*([\w]+)?\s*(```((.|\n)*)```)");

                                if (match.Success)
                                {
                                    var lang = match.Groups[1].Success ? match.Groups[1].Value : "urcl";
                                    var outputType = match.Groups[2].Success ? match.Groups[2].Value : "emulate";
                                    var tier = match.Groups[3].Success ? match.Groups[3].Value : "any";

                                    Jobs.Enqueue(new BotTask(m, lang, outputType, tier, match.Groups[5].Value));
                                    Sleep.Set();
                                }
                                else
                                {
                                    Reply(m, "Code block was not specified.");
                                }
                            }
                        }
                    }
                }
                else
                {
                    output("Failed to obtain self ID.");
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
