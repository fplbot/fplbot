using System;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Formatting;
using FplBot.Slack.Abstractions;
using Microsoft.Extensions.Logging;

namespace FplBot.Slack.Helpers
{
    internal class EntryForGameweek: IEntryForGameweek
    {

        private readonly IEntryClient _entryClient;
        private readonly ILogger<EntryForGameweek> _logger;

        public EntryForGameweek(IEntryClient entryClient, ILogger<EntryForGameweek> logger)
        {
            _entryClient = entryClient;
            _logger = logger;
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
                _logger.LogError(e.Message, e);
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
                _logger.LogError(e.Message, e);
                return null;
            }
        }
    }
}
