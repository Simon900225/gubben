using Discord;
using DiscordGubbBot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordGubbBot.Storage
{
    internal class InMemoryStorage
    {
        public static List<Emoji> OrderedEmojiList = new() { new Emoji("1️⃣"), new Emoji("2️⃣"), new Emoji("3️⃣"), new Emoji("4️⃣"), new Emoji("5️⃣"), new Emoji("6️⃣"), new Emoji("7️⃣"), new Emoji("8️⃣"), new Emoji("9️⃣") };

        public static Dictionary<ulong, Poll> Polls = new();

        public static Dictionary<ulong, Dictionary<ulong, Reaction>> ReactionTimes = new Dictionary<ulong, Dictionary<ulong, Reaction>>();

        public static Dictionary<ulong, List<Reaction>> MessageReactions = new Dictionary<ulong, List<Reaction>>();
    }
}
