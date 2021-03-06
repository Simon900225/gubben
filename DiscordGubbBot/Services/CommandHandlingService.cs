using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using FluentScheduler;
using DiscordGubbBot.Modules;
using DiscordGubbBot.Model;
using DiscordGubbBot.Storage;

namespace DiscordGubbBot.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;

        private readonly string voting_morning_template = "God morgon gubbar!\nVilka ska vara med och spela lunch-cs idag?\n";

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            // Hook CommandExecuted to handle post-command-execution logic.
            _commands.CommandExecuted += CommandExecutedAsync;
            // Hook MessageReceived so we can process each message to see
            // if it qualifies as a command.
            _discord.MessageReceived += MessageReceivedAsync;
            _discord.ReactionAdded += ReactionAdded;
            _discord.ReactionRemoved += ReactionRemoved;

            JobManager.Initialize();

//Lunch-cs är ett minne blott
//            JobManager.AddJob(
//                async () => {
//#if DEBUG
//                    var channel = _discord.GetChannel(Channels.DEV) as ITextChannel;
//#else
//                    var channel = _discord.GetChannel(Channels.CSGaming) as ITextChannel;
//#endif
//                    var message = await channel?.SendMessageAsync(voting_template);
                    
//                },
//                s => s.ToRunEvery(0).Days().At(8, 0).WeekdaysOnly()
//            );

        }

        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> userMessage, ISocketMessageChannel messageChannel, SocketReaction reaction)
        {
            await UpdateMessage(false, userMessage, messageChannel, reaction);
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> userMessage, ISocketMessageChannel messageChannel, SocketReaction reaction)
        {
            await UpdateMessage(true, userMessage, messageChannel, reaction);
        }

        private async Task UpdateMessage(bool added, Cacheable<IUserMessage, ulong> userMessage, ISocketMessageChannel messageChannel, SocketReaction reaction)
        {
            var message = await userMessage.GetOrDownloadAsync();

            string newContent = "";

            if (message != null)
            {
                if (message.Content.Contains("Vilka ska vara med och spela lunch-cs idag?") || message.Content.ToLower().Contains("vilka vill vara med?"))
                {
                    newContent = await UpdateGameMessage(added, messageChannel, reaction, message);
                }
                else if (message.Content.Contains("Vad tycker ni?"))
                {
                    newContent = await UpdatePoll(added, messageChannel, reaction, message);
                }
                else if (message.Content.Contains("Vilka ska vara med?") || message.Content.Contains("Vilka vill vara med på"))
                {
                    newContent = message.Content.Split('?').FirstOrDefault() ?? "";
                    newContent += "?\n";

                    var user = await messageChannel.GetUserAsync(reaction.UserId);
                    
                    ToggleReactionTime(added, message.Id, user.Id);
                    
                    foreach (var r in InMemoryStorage.ReactionTimes[message.Id].OrderBy(x => x.Value.Time))
                    {
                        user = await messageChannel.GetUserAsync(r.Key);
                        
                        IGuildUser guildUser = (IGuildUser)user;
                        var nickname = string.IsNullOrEmpty(guildUser?.Nickname) || guildUser?.Nickname == user.Username ? string.Empty : $"({guildUser.Nickname})";

                        newContent += $"{user.Username} {nickname}\n";
                    }
                }
            }

            if (!string.IsNullOrEmpty(newContent))
            {
                await message.ModifyAsync((x) =>
                {
                    x.Content = newContent;
                });
            }
        }

        private void ToggleReactionTime(bool added, ulong messageId, ulong userId, Emoji emoji = null)
        {
            if (!InMemoryStorage.ReactionTimes.ContainsKey(messageId))
                InMemoryStorage.ReactionTimes.Add(messageId, new Dictionary<ulong, Reaction>());

            if (added)
            {
                if (!InMemoryStorage.ReactionTimes[messageId].ContainsKey(userId))
                    InMemoryStorage.ReactionTimes[messageId].Add(userId, new Reaction(userId, DateTime.Now, emoji));
            }
            else
            {
                if (InMemoryStorage.ReactionTimes[messageId].ContainsKey(userId))
                    InMemoryStorage.ReactionTimes[messageId].Remove(userId);
            }
        }

        private async Task<string> UpdatePoll(bool added, ISocketMessageChannel messageChannel, SocketReaction reaction, IUserMessage message)
        {
            var newContent = message.Content;
            var poll = InMemoryStorage.Polls[message.Id];

            if (poll != null)
            {
                var user = await messageChannel.GetUserAsync(reaction.UserId);
                var emoji = new Emoji(reaction.Emote.Name);

                if (!InMemoryStorage.MessageReactions.ContainsKey(message.Id))
                {
                    InMemoryStorage.MessageReactions.Add(message.Id, new List<Reaction>());
                }

                if(added)
                    InMemoryStorage.MessageReactions[message.Id].Add(new Reaction(user.Id, DateTime.Now, emoji));
                else
                {
                    var toRemove = InMemoryStorage.MessageReactions[message.Id].FirstOrDefault(x => x.UserID == user.Id && x.Emoji.Name == emoji.Name);
                    if (toRemove != null)
                        InMemoryStorage.MessageReactions[message.Id].Remove(toRemove);
                }

                newContent = $"Vad tycker ni? {poll.Question}\n";

                foreach (var alternative in poll.Alternatives)
                {
                    var alternativeReactions = InMemoryStorage.MessageReactions[message.Id].Where(x => x.Emoji.Name == alternative.Emoji.Name).OrderBy(x => x.Time);
                    newContent += $"{alternative.Emoji} {alternative.Text} - {alternativeReactions.Count()}st";
                    bool first = true;
                    foreach (var r in alternativeReactions)
                    {
                        if (!first)
                        {
                            newContent += ",";
                        }
                        if (first)
                        {
                            first = false;
                            newContent += " :";
                        }

                        var reactUser = await messageChannel.GetUserAsync(r.UserID);

                        IGuildUser guildUser = (IGuildUser)reactUser;
                        var nickname = string.IsNullOrEmpty(guildUser?.Nickname) || guildUser?.Nickname == reactUser.Username ? string.Empty : $"({guildUser.Nickname})";

                        newContent += $" {reactUser.Username} {nickname}";
                    }
                    newContent += "\n";
                }
            }
            return newContent;
        }


        private async Task<string> UpdateGameMessage(bool added, ISocketMessageChannel messageChannel, SocketReaction reaction, IUserMessage message)
        {
            var newContent = message.Content.Split('?').FirstOrDefault() ?? "";
            newContent += "?\n";

            var user = await messageChannel.GetUserAsync(reaction.UserId);

            ToggleReactionTime(added, message.Id, user.Id);

            int i = 1;
            foreach (var r in InMemoryStorage.ReactionTimes[message.Id].OrderBy(x => x.Value.Time))
            {
                user = await messageChannel.GetUserAsync(r.Key);
                if (i == 6)
                {
                    newContent += "Och på avbytarbänken har vi:\n";
                }

                IGuildUser guildUser = (IGuildUser)user;
                var nickname = string.IsNullOrEmpty(guildUser?.Nickname) || guildUser?.Nickname == user.Username ? string.Empty : $"({guildUser.Nickname})";

                newContent += $"{i}. {user.Username} {nickname}: {r.Value.Time:HH:mm:ss}\n";

                i++;
            }

            return newContent;
        }

        public async Task InitializeAsync()
        {
            // Register modules that are public and inherit ModuleBase<T>.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // Ignore system messages, or messages from other bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            // This value holds the offset where the prefix ends
            var argPos = 0;
            // Perform prefix check. You may want to replace this with
            // (!message.HasCharPrefix('!', ref argPos))
            // for a more traditional command format like !help.
            if (message.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            { 
                var context = new SocketCommandContext(_discord, message);
                // Perform the execution of the command. In this method,
                // the command service will perform precondition and parsing check
                // then execute the command if one is matched.
                var result = await _commands.ExecuteAsync(context, argPos, _services);
                
                // Note that normally a result will be returned by this format, but here
                // we will handle the result in CommandExecutedAsync,
                if (!result.IsSuccess)
                {
                    DialowFlowService dialogflow = new DialowFlowService("123", "sunfleetangulart-1485335477034");
                    var dialogflowQueryResult = await dialogflow.CheckIntent(message.Content);
                    await message.Channel.SendMessageAsync(dialogflowQueryResult.FulfillmentText);
                }
            }
            else
            {
                //var refer = rawMessage.Reference.MessageId; //id of referenced message.(Store todays message as static id)
                var context = new SocketCommandContext(_discord, message);
                // Perform the execution of the command. In this method,
                // the command service will perform precondition and parsing check
                // then execute the command if one is matched.
                //await _commands.ExecuteAsync(context, argPos, _services);
                // Note that normally a result will be returned by this format, but here
                // we will handle the result in CommandExecutedAsync,
            }
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // command is unspecified when there was a search failure (command not found); we don't care about these errors
            if (!command.IsSpecified)
                return;

            // the command was successful, we don't care about this result, unless we want to log that a command succeeded.
            if (result.IsSuccess)
                return;

            // the command failed, let's notify the user that something happened.
            await context.Channel.SendMessageAsync($"error: {result}");
        }
    }
}
