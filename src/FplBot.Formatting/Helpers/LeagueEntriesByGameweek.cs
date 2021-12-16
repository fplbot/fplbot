using System.Collections.Concurrent;
using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;

namespace FplBot.Formatting.Helpers;

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
                var gameweekEntries = await _entryForGameweek.GetEntryForGameweek((ClassicLeagueEntry)entry, gw);
                entryDictionary.Add(gameweekEntries);
            }));

            return entryDictionary;
        }
        catch (HttpRequestException hre) when (LogWarning(hre, gw, leagueId))
        {
            return Enumerable.Empty<GameweekEntry>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return Enumerable.Empty<GameweekEntry>();
        }
    }
    private bool LogWarning(HttpRequestException hre, int gw, int leagueId)
    {
        _logger.LogWarning("Could not get entries in {GW} for {LeagueId}", gw, leagueId);
        return hre.StatusCode == HttpStatusCode.NotFound;
    }

}
