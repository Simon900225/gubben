using Discord;
using System.Collections.Generic;

namespace DiscordGubbBot.Model
{
    public class Poll
    {
        public string Question { get; set; }
        public List<Alternative> Alternatives { get; set; } = new List<Alternative>();
        public ulong MessageID { get; internal set; }
    }
}
