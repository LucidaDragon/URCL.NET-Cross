using Discord;
using Discord.WebSocket;
using System.Net;
using System.Threading.Tasks;

namespace UrclBot
{
    public class BotTask
    {
        public SocketMessage Source { get; }
        public string Name { get; }

        public async Task<string> GetContent()
        {
            if (FixedContent is null)
            {
                using var fetch = new WebClient();
                return await fetch.DownloadStringTaskAsync(Attachment.Url);
            }
            else
            {
                return FixedContent;
            }
        }

        private readonly string FixedContent;
        private readonly Attachment Attachment;

        public BotTask(SocketMessage source, string content)
        {
            Source = source;
            FixedContent = content;
            Name = "User Task";
        }

        public BotTask(SocketMessage source, Attachment content)
        {
            Source = source;
            Attachment = content;
            Name = content.Filename;
        }
    }
}
