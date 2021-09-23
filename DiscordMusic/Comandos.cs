using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordMusic
{


    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        private DiscordSocketClient _client;
        private CommandHandler _handler;
        private SocketGuild _guild;
        private SocketVoiceChannel _voiceChannel;

        // ~say hello world -> hello world
        [Command("say", RunMode = RunMode.Async)]
        [Summary("Echoes a message.")]
        public Task SayAsync(string echo)
        {
            return Context.Channel.SendMessageAsync(echo);
        }

        // ReplyAsync is a method on ModuleBase 

        [Command("play", RunMode = RunMode.Async)]
        [Summary("PLAYS A LINK")]
        public Task PlayAsync(string echo)
        {
            return Context.Channel.SendMessageAsync(echo);
        }

        [Command("join", RunMode = RunMode.Async)]
        public async Task JoinChannel(string link, IVoiceChannel channel = null)
        {
            try
            {
                // Get the audio channel
                channel = channel ?? (Context.User as IGuildUser)?.VoiceChannel;
                if (channel == null) { await Context.Channel.SendMessageAsync("User must be in a voice channel, or a voice channel must be passed as an argument."); return; }

                // For the next step with transmitting audio, you would want to pass this Audio Client in to a service.
                var audioClient = await channel.ConnectAsync();

                using (var ffmpeg = CreateStream(link))
                using (var output = ffmpeg.StandardOutput.BaseStream)
                using (var discord = audioClient.CreatePCMStream(AudioApplication.Mixed))
                {
                    try { await output.CopyToAsync(discord); }
                    finally { await discord.FlushAsync(); }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [Command("rem", RunMode = RunMode.Async)]
        public async Task Reminder(string Data, string Users)
        {
            var reminder = new Reminder();

            DateTime Date;
            if (!DateTime.TryParse(Data, out Date))
            {
                await Context.Channel.SendMessageAsync("Data inválida, utilize data no formato: \"dd/mm/yyyy HH:MM\"");
                return;
            }

            reminder.Date = Date;
            if (Users == "")
                reminder.Users = new List<string> { Context.User.ToString() };
            else
                reminder.Users.AddRange(Users.Split(','));
            
            reminder.Server = Context.Guild.Id.ToString();
            reminder.Channel = Context.Channel.Id.ToString();


            if (!File.Exists("reminders.json"))
                File.Create("reminders.json");
            try
            {
                string json;
                var inStream = new FileStream("reminders.json", FileMode.Open,
                               FileAccess.ReadWrite, FileShare.ReadWrite);
                using (StreamReader reader = new StreamReader(inStream))
                {
                    json = reader.ReadToEnd();
                }


                var reminders = JsonConvert.DeserializeObject<List<Reminder>>(json);
                if (reminders == null)
                    reminders = new List<Reminder>();
                reminders.Add(reminder);

                json = JsonConvert.SerializeObject(reminders);

                var truStream = new FileStream("reminders.json", FileMode.Truncate,
                               FileAccess.ReadWrite, FileShare.ReadWrite);
                using (StreamWriter r = new StreamWriter(truStream))
                {

                }

                var outStream = new FileStream("reminders.json", FileMode.Append,
                               FileAccess.Write, FileShare.ReadWrite);
                using (StreamWriter r = new StreamWriter(outStream))
                {
                    r.Write(json);
                }
            }
            catch (Exception e)
            { }
        }

        

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }

        private async Task SendAsync(IAudioClient client, string path)
        {
            // Create FFmpeg using the previous example
            using (var ffmpeg = CreateStream(path))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = client.CreatePCMStream(AudioApplication.Mixed))
            {
                try { await output.CopyToAsync(discord); }
                finally { await discord.FlushAsync(); }
            }
        }

    }
}
