using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordGubbBot.Model;
using DiscordGubbBot.Services;
using DiscordGubbBot.Storage;
using Microsoft.Extensions.Configuration;
using Renci.SshNet;

namespace DiscordGubbBot.Modules
{
    // Modules must be public and inherit from an IModuleBase
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        private readonly string Password = Environment.GetEnvironmentVariable("SSH_PASSWORD");
        private readonly string Username = Environment.GetEnvironmentVariable("SSH_USERNAME");
        private readonly string Host = Environment.GetEnvironmentVariable("SSH_URL");
        readonly Random random = new();

        // Dependency Injection will fill this value in for us
        public PictureService PictureService { get; set; }
        //public IConfiguration Configuration { get; set; } //Crashes application?

        [Command("match")]
        [Alias("game")]
        public async Task StartMatch(string time = "")
        {
            if (time.Substring(0,5).Contains(":"))
                await ReplyAsync($"Match kl {time.Substring(0, 5)} vilka vill vara med?");
            else if (int.TryParse(time, out int t) && t < 2359)
                await ReplyAsync($"Match kl {t.ToString().Insert(2, ":")} vilka vill vara med?");
            else if (time.Contains("lunch"))
                await ReplyAsync($"Vilka ska vara med och spela lunch-cs idag?");
            else
                await ReplyAsync($"Match nu! Vilka vill vara med?");

            if (random.Next(0,20) == 0)
            {
                await ReplyAsync($"Jag kan tyvärr inte vara med den här gången. Men GLHF!");
            }
        }

        [Command("vote")]
        [Alias("rösta")]
        public async Task Vote(params string[] alternativesStrings)
        {
            var alternatives = alternativesStrings.ToList();

            var poll = new Poll();
            var question = string.Empty;
            if (alternativesStrings.FirstOrDefault().StartsWith("q="))
            {
                poll.Question = alternativesStrings.FirstOrDefault();
                alternatives = alternatives.Skip(1).ToList();
            }

            if (alternatives.Count > 9)
            {
                await ReplyAsync($"Så många alternativ kan du väl inte ha?");
            }
            else
            {
                for (int i = 0; i < alternatives.Count; i++)
                {
                    poll.Alternatives.Add(new Alternative() { Emoji = InMemoryStorage.OrderedEmojiList[i], Text = alternatives[i] });
                }

                var message = await ReplyAsync($"Vad tycker ni? {question} {(string.IsNullOrEmpty(question) ? "?": "")}\n" + string.Join("\n", poll.Alternatives.Select(x => x.Emoji + " " + x.Text)));
                poll.MessageID = message.Id;

                InMemoryStorage.Polls.Add(message.Id, poll);
            }
        }

        [Command("attendance")]
        [Alias("handuppräckning")]
        public async Task Attendance (string eventName = "")
        {
            if (string.IsNullOrEmpty(eventName))
                await ReplyAsync("Vilka ska vara med?");
            else
                await ReplyAsync($"Vilka vill vara med på {eventName}?");
        }

        //TODO; Add info attribute that explains the command.
        [Command("vad kan du göra?")]
        [Alias("help", "hjälp")]
        public async Task CanDoInfo()
        {
            StringBuilder sb = new();
            int i = 1;
            foreach (var method in typeof(PublicModule).GetMethods())
            {
                var commandAttributes = method.GetCustomAttributes().Where(x => x.GetType() == typeof(CommandAttribute)).ToList();
                var aliasAttributes = method.GetCustomAttributes().Where(x => x.GetType() == typeof(AliasAttribute)).ToList();
                if (commandAttributes.Count > 0)
                {
                    sb.Append($"{i}. '");

                    sb.Append(string.Join("', '", commandAttributes.Cast<CommandAttribute>().Select(x => x.Text)));

                    if (aliasAttributes.Count > 0)
                        sb.Append("', '");

                    sb.Append(string.Join("', '", aliasAttributes.Cast<AliasAttribute>().SelectMany(x => x.Aliases).Select(x => x.ToString())));
                    sb.Append("'\n");
                    i++;
                }


            }
            await ReplyAsync(sb.ToString());
        }

        [Command("update valheim")]
        [Alias("uppdatera valheim", "starta om valheim", "restart valheim")]
        public async Task UpdateValheim()
        {
            var ConnNfo = new ConnectionInfo(Host, 22, Username,
                new AuthenticationMethod[]{
                    new PasswordAuthenticationMethod(Username, Password),
                }
            );

            using (var sshclient = new SshClient(ConnNfo))
            {
                sshclient.Connect();
                using var stream = sshclient.CreateShellStream("xterm", 255, 50, 800, 600, 1024);
                stream.Write("sudo docker restart dockercomposes_valheim_1\n");
                stream.Expect($"password for {Username}:");
                stream.Write(Password + "\n");

                sshclient.Disconnect();
            }

            await ReplyAsync("I fix. Vänta ca 2 min så ska servern vara igång igen.");
        }

        [Command("dox")]
        public async Task DoxUserAsync(IUser user = null)
        {
            user = user ?? Context.User;

            await ReplyAsync(user.ToString());
        }

        [Command("ping")]
        [Alias("pong", "hello")]
        public Task PingAsync()
            => ReplyAsync("pong!");


        [Command("Öl")]
        [Alias("gt", "punsch", "sprit", "vin", "gin")]
        public Task CheersAsync()
            => ReplyAsync("Skål mina vänner!");

        [Command("cat")]
        public async Task CatAsync()
        {
            // Get a stream containing an image of a cat
            var stream = await PictureService.GetCatPictureAsync();
            // Streams must be seeked to their beginning before being uploaded!
            stream.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(stream, "cat.png");
        }

        [Command("animal")]
        [Alias("djur")]
        public async Task AnimalAsync(string animal = "")
        {
            // Get a stream containing an image of a cat
            var stream = await PictureService.GetPictureAsync(animal);
            // Streams must be seeked to their beginning before being uploaded!
            stream.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(stream, "animal.png");
        }

        // Get info on a user, or the user who invoked the command if one is not specified
        [Command("userinfo")]
        public async Task UserInfoAsync(IUser user = null)
        {
            user = user ?? Context.User;

            await ReplyAsync(user.ToString());
        }

        // Ban a user
        [Command("ban")]
        [RequireContext(ContextType.Guild)]
        // make sure the user invoking the command can ban
        [RequireUserPermission(GuildPermission.BanMembers)]
        // make sure the bot itself can ban
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task BanUserAsync(IGuildUser user, [Remainder] string reason = null)
        {
            await user.Guild.AddBanAsync(user, reason: reason);
            await ReplyAsync("ok!");
        }

        // [Remainder] takes the rest of the command's arguments as one argument, rather than splitting every space
        [Command("echo")]
        public Task EchoAsync([Remainder] string text)
            // Insert a ZWSP before the text to prevent triggering other bots!
            => ReplyAsync('\u200B' + text);

        // 'params' will parse space-separated elements into a list
        [Command("list")]
        public Task ListAsync(params string[] objects)
            => ReplyAsync("You listed: " + string.Join("; ", objects));

        // Setting a custom ErrorMessage property will help clarify the precondition error
        [Command("guild_only")]
        [RequireContext(ContextType.Guild, ErrorMessage = "Sorry, this command must be ran from within a server, not a DM!")]
        public Task GuildOnlyCommand()
            => ReplyAsync("Nothing to see here!");
    }
}
