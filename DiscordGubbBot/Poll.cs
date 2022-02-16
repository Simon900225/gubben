using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordGubbBot
{
    public class Alternative
    {
        public Emoji Emoji { get; set; }
        public string Text { get; set; }
    }
    public class Poll
    {
        public string Question { get; set; }
        public List<Alternative> Alternatives { get; set; } = new List<Alternative>();
        public ulong MessageID { get; internal set; }
    }
}
