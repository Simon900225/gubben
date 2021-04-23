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

namespace DiscordGubbBot.Services
{
    public class CommandHandlingService
    {
        private readonly Dictionary<ulong, Dictionary<ulong, DateTime>> reactionTimes = new Dictionary<ulong, Dictionary<ulong, DateTime>>();
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;

        private readonly string voting_template = "God morgon gubbar!\nVilka ska vara med och spela lunch-cs idag?\n";

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
            _discord.ReactionRemoved += _discord_ReactionRemoved;

            JobManager.Initialize();

            JobManager.AddJob(
                () => {
#if DEBUG
                    var channel = _discord.GetChannel(Channels.DEV) as ITextChannel;
#else
                    var channel = _discord.GetChannel(Channels.CSGaming) as ITextChannel;
#endif
                    channel?.SendMessageAsync(voting_template);
                },
                s => s.ToRunEvery(0).Days().At(8, 0).WeekdaysOnly()
            );

        }

        private async Task _discord_ReactionRemoved(Cacheable<IUserMessage, ulong> userMessage, ISocketMessageChannel messageChannel, SocketReaction reaction)
        {
            await UpdateMessage(false, userMessage, messageChannel, reaction);
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> userMessage, ISocketMessageChannel messageChannel, SocketReaction reaction)
        {
            await UpdateMessage(true, userMessage, messageChannel, reaction);
        }

        private async Task UpdateMessage(bool added, Cacheable<IUserMessage, ulong> userMessage, ISocketMessageChannel messageChannel, SocketReaction reaction)
        {
#if DEBUG
            if (messageChannel.Id == Channels.DEV)
#else
            if (messageChannel.Id == Channels.CSGaming)
#endif
            {
                var message = await userMessage.GetOrDownloadAsync();

                if (message?.Content.Contains("Vilka ska vara med och spela lunch-cs idag?") ?? false)
                {
                    var user = await messageChannel.GetUserAsync(reaction.UserId);

                    if (!reactionTimes.ContainsKey(message.Id))
                        reactionTimes.Add(message.Id, new Dictionary<ulong, DateTime>());

                    if (added)
                    {
                        if (!reactionTimes[message.Id].ContainsKey(user.Id))
                            reactionTimes[message.Id].Add(user.Id, DateTime.Now);
                    }
                    else
                    {
                        if (reactionTimes[message.Id].ContainsKey(user.Id))
                            reactionTimes[message.Id].Remove(user.Id);
                    }

                    var newContent = voting_template;

                    int i = 1;
                    foreach (var r in reactionTimes[message.Id].OrderBy(x => x.Value))
                    {
                        user = await messageChannel.GetUserAsync(r.Key);
                        if (i == 6)
                        {
                            newContent += "Och på avbytarbänken har vi:\n";
                        }

                        IGuildUser guildUser = (IGuildUser)user;
                        var nickname = string.IsNullOrEmpty(guildUser?.Nickname) || guildUser?.Nickname == user.Username ? string.Empty : $"({guildUser.Nickname})";

                        newContent += $"{i.ToString()}. {user.Username} {nickname}: {r.Value.ToString("HH:mm:ss")}\n";

                        i++;
                    }

                    await message.ModifyAsync((x) =>
                    {
                        x.Content = newContent;
                    });
                }
            }
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
                await _commands.ExecuteAsync(context, argPos, _services);
                // Note that normally a result will be returned by this format, but here
                // we will handle the result in CommandExecutedAsync,

                DialowFlowService dialogflow = new DialowFlowService("123", "sunfleetangulart-1485335477034");
                var dialogflowQueryResult = await dialogflow.CheckIntent(message.Content);
                await message.Channel.SendMessageAsync(dialogflowQueryResult.FulfillmentText);
            }
            else
            {
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
