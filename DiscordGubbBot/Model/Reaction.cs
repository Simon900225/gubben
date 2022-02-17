using Discord;
using System;

namespace DiscordGubbBot.Model
{
    class Reaction
    {
        public Reaction(ulong userID, DateTime time, Emoji emoji = null)
        {
            UserID = userID;
            Time = time;
            Emoji = emoji;
        }

        public DateTime Time { get; set; }
        public Emoji Emoji { get; set; }
        public ulong UserID { get; set; }
    }
}
