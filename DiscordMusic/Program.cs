using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordMusic
{
    class Program
    {


        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private readonly CommandService _commands;
        private CommandHandler _commandHandler;


        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();


            _client.Log += Log;

            var _commands = new CommandService();

            var services = new ServiceCollection()
                .AddSingleton(this)
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();


            var commandHandler = new CommandHandler(_client, _commands);
            commandHandler.InstallCommandsAsync();



            //  You can assign your bot token to a string, and pass that in to connect.
            //  This is, however, insecure, particularly if you plan to have your code hosted in a public repository.
            var token = "ODg3MDExNjU0Nzg3ODA5MzMw.YT97-g.SEDJIrCnv8M9iynQA3uZsN0QZYw";

            // Some alternative options would be to keep your token in an Environment Variable or a standalone file.
            // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
            // var token = File.ReadAllText("token.txt");
            // var token = JsonConvert.DeserializeObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            Task.Run(() => SendReminder());

            // Block this task until the program is closed.
            await Task.Delay(-1);


        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task SendReminder()
        {

            while (true)
            {
                try
                {
                    string json;
                    var inStream = new FileStream("reminders.json", FileMode.Open,
                               FileAccess.ReadWrite, FileShare.ReadWrite);
                    using (StreamReader reader = new StreamReader(inStream))
                    {
                        json = reader.ReadToEnd();
                    }

                    if (json != "")
                    {
                        var reminders = JsonConvert.DeserializeObject<List<Reminder>>(json);

                        foreach (Reminder rem in reminders)
                        {
                            if (rem.Date <= DateTime.Today)
                            {
                                string mention = "";
                                foreach (string user in rem.Users)
                                    mention += user;
                                await _client.
                                    GetGuild(Convert.ToUInt64(rem.Server)).
                                    GetTextChannel(Convert.ToUInt64(rem.Channel)).
                                    SendMessageAsync($"Lembrete de compromisso! {mention}");
                            }
                        }

                        reminders.RemoveAll(a => a.Date <= DateTime.Today);

                        var truStream = new FileStream("reminders.json", FileMode.Truncate,
                               FileAccess.ReadWrite, FileShare.ReadWrite);
                        using (StreamWriter r = new StreamWriter(truStream))
                        {

                        }

                        if (reminders.Count > 0)
                        {
                            json = JsonConvert.SerializeObject(reminders);
                            var outStream = new FileStream("reminders.json", FileMode.Append,
                                   FileAccess.Write, FileShare.ReadWrite);
                            using (StreamWriter r = new StreamWriter(outStream))
                            {
                                r.Write(json);
                            }
                        }
                    }

                    Thread.Sleep(2000);
                }
                catch (Exception e)
                {
                    Thread.Sleep(2000);
                }
            }
        }

    }
}
