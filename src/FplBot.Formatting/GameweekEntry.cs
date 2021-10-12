using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;

namespace FplBot.Formatting
{
    public class GameweekEntry
    {
        public GameweekEntry () { }

        public GameweekEntry(int entryId, string playerName, string realName, EntryPicks entryPicks)
        {
            EntryId = entryId;
            EntryName = playerName;
            EntryRealName = realName;
            EventEntryHistory = entryPicks.EventEntryHistory;
            Picks = entryPicks.Picks;
            ActiveChip = entryPicks.ActiveChip;
        }

        public int EntryId { get; set; }
        public string EntryName { get; set; }
        public string EntryRealName { get; set; }

        public EventEntryHistory EventEntryHistory { get; set; }

        public ICollection<Pick> Picks { get; set; }

        public string ActiveChip { get; set; }

        public int Bank { get { return EventEntryHistory.Bank; } }

        public int TotalValue { get { return EventEntryHistory.Value; } }

        public Pick Captain { get { return Picks.Single(pick => pick.IsCaptain); } }

        public Pick ViceCaptain { get { return Picks.Single(pick => pick.IsViceCaptain); } }
    }
}