using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal class EntryForGameweek: IEntryForGameweek
    {

        private readonly IEntryClient _entryClient;

        public EntryForGameweek(IEntryClient entryClient)
        {
            _entryClient = entryClient;
        }

        public async Task<GameweekEntry> GetEntryForGameweek(ClassicLeagueEntry entry, int gameweek)
        {
            try
            {
                var entryPicksTask = _entryClient.GetPicks(entry.Entry, gameweek);
                var entryPicks = await entryPicksTask;

                return new GameweekEntry(entry.Entry, entry.PlayerName, entry.EntryName, entryPicks);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public async Task<GameweekEntry> GetEntryForGameweek(GenericEntry entry, int gameweek)
        {
            try
            {
                var entryPicks = await _entryClient.GetPicks(entry.Entry, gameweek);

                return new GameweekEntry(entry.Entry, entry.EntryName, entry.EntryName, entryPicks);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
    }

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
