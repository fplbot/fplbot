using Fpl.Client.Abstractions;
using Fpl.Client.Models;

namespace FplBot.WebApi.Endpoints.Api.Fpl;

public static class FplEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/leagues/{leagueId:int}", GetLeague);
        group.MapGet("/leagues/{leagueId:int}/details", GetLeagueDetails);
    }

    private static async Task<IResult> GetLeague(int leagueId, ILeagueClient leagueClient, ILogger<Program> logger)
    {
        try
        {
            var league = await leagueClient.GetClassicLeague(leagueId);
            if (league == null) return TypedResults.NotFound();
            return TypedResults.Ok(new
            {
                LeagueName = league.Properties?.Name,
                LeagueAdmin = league.Standings?.Entries.FirstOrDefault(e => e.Entry == league.Properties?.AdminEntry)?.PlayerName
            });
        }
        catch (HttpRequestException e)
        {
            logger.LogWarning(e.ToString());
        }

        return TypedResults.NotFound();
    }

    private static async Task<IResult> GetLeagueDetails(
        int leagueId,
        ILeagueClient leagueClient,
        IEntryClient entryClient,
        ITransfersClient transfersClient,
        IEntryHistoryClient entryHistoryClient,
        IGlobalSettingsClient globalSettingsClient,
        ILogger<Program> logger)
    {
        try
        {
            var league = await leagueClient.GetClassicLeague(leagueId);
            if (league?.Standings == null) return TypedResults.NotFound();

            var settings = await globalSettingsClient.GetGlobalSettings();
            var currentGw = settings?.Gameweeks.GetCurrentGameweek();
            var playersById = settings?.Players.ToDictionary(p => p.Id) ?? new Dictionary<int, Player>();

            var summaries = new List<object>();
            if (currentGw != null)
            {
                foreach (var entry in league.Standings.Entries)
                {
                    var transfersTask = transfersClient.GetTransfers(entry.Entry);
                    var historyTask = entryHistoryClient.GetHistory(entry.Entry);
                    var picksTask = entryClient.GetPicks(entry.Entry, currentGw.Id);
                    await Task.WhenAll(transfersTask, historyTask, picksTask);

                    var transfers = transfersTask.Result ?? [];
                    var history = historyTask.Result;
                    var picks = picksTask.Result;

                    var chip = history?.entryHistory.Chips.FirstOrDefault(c => c.Event == currentGw.Id);
                    var captainPick = picks?.Picks.FirstOrDefault(p => p.IsCaptain);
                    var viceCaptainPick = picks?.Picks.FirstOrDefault(p => p.IsViceCaptain);

                    summaries.Add(new
                    {
                        entry = entry.Entry,
                        playerName = entry.PlayerName,
                        captain = captainPick != null && playersById.TryGetValue(captainPick.PlayerId, out var cp) ? cp.WebName : null,
                        viceCaptain = viceCaptainPick != null && playersById.TryGetValue(viceCaptainPick.PlayerId, out var vcp) ? vcp.WebName : null,
                        chip = chip?.Name,
                        transfers = transfers.Where(t => t.Event == currentGw.Id).Select(t => new
                        {
                            playerIn = playersById.GetValueOrDefault(t.ElementIn)?.WebName,
                            playerOut = playersById.GetValueOrDefault(t.ElementOut)?.WebName,
                            playerInCost = t.ElementInCost / 10.0,
                            playerOutCost = t.ElementOutCost / 10.0,
                            time = t.Time
                        })
                    });
                }
            }

            return TypedResults.Ok(new
            {
                leagueName = league.Properties?.Name,
                leagueAdmin = league.Standings.Entries.FirstOrDefault(e => e.Entry == league.Properties?.AdminEntry)?.PlayerName,
                gameweek = currentGw?.Id,
                standings = league.Standings.Entries.Select(e => new
                {
                    entry = e.Entry,
                    playerName = e.PlayerName,
                    teamName = e.EntryName,
                    rank = e.Rank,
                    lastRank = e.LastRank,
                    total = e.Total,
                    eventTotal = e.EventTotal
                }),
                summaries
            });
        }
        catch (HttpRequestException e)
        {
            logger.LogWarning(e.ToString());
        }

        return TypedResults.NotFound();
    }
}
