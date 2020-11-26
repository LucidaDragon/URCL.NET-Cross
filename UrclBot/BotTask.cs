using Discord;
using Discord.WebSocket;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace UrclBot
{
    public class BotTask
    {
        public SocketMessage Source { get; }
        public string Name { get; }
        public string Language { get; }

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

        public BotTask(SocketMessage source, string language, string content)
        {
            Source = source;
            Language = language;
            FixedContent = content;
            Name = $"{language} Task";
        }

        public BotTask(SocketMessage source, Attachment content)
        {
            Source = source;
            Language = Path.GetExtension(content.Filename);
            Attachment = content;
            Name = content.Filename;
        }
    }
}
