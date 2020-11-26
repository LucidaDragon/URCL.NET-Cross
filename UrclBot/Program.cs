using System;
using System.IO;
using System.Threading;

namespace UrclBot
{
    class Program
    {
        static void Main(string[] args)
        {
            if (File.Exists("URCL.NET.exe") && File.Exists("token.txt"))
            {
                var urcl = new UrclInterface("URCL.NET.exe", 11113);
                urcl.Configuration.Add("Emulate");
                urcl.Configuration.Add("DisableBreak");
                var bot = new Bot(urcl, Console.WriteLine, File.ReadAllText("token.txt"));
                bot.Start();
                new AutoResetEvent(false).WaitOne();
            }
            else
            {
                Console.WriteLine("Missing required files. (URCL.NET.exe and token.txt)");
            }
        }
    }
}
