using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using FplBot.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace FplBot.Core.Helpers
{
    public class LeagueEntriesByGameweek : ILeagueEntriesByGameweek
    {
        private readonly ILeagueClient _leagueClient;
        private readonly IEntryForGameweek _entryForGameweek;
        private readonly ILogger<LeagueEntriesByGameweek> _logger;

        public LeagueEntriesByGameweek(ILeagueClient leagueClient, IEntryForGameweek entryForGameweek, ILogger<LeagueEntriesByGameweek> logger)
        {
            _leagueClient = leagueClient;
            _entryForGameweek = entryForGameweek;
            _logger = logger;
        }

        public async Task<IEnumerable<GameweekEntry>> GetEntriesForGameweek(int gw, int leagueId)
        {
            try
            {
                var league = await _leagueClient.GetClassicLeague(leagueId);
            
                var entries = league.Standings.Entries;

                var entryDictionary = new ConcurrentBag<GameweekEntry>();

                await Task.WhenAll(entries.Select(async entry =>
                {
                    var gameweekEntries = await _entryForGameweek.GetEntryForGameweek(entry, gw);
                    entryDictionary.Add(gameweekEntries);
                }));

                return entryDictionary;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return Enumerable.Empty<GameweekEntry>();
            }
        }

    }
}