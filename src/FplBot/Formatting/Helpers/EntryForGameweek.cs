using Fpl.Client.Abstractions;
using Fpl.Client.Models;

namespace FplBot.Formatting.Helpers;

public class EntryForGameweek(IEntryClient entryClient, ILogger<EntryForGameweek> logger) : IEntryForGameweek
{
    public async Task<GameweekEntry?> GetEntryForGameweek(ClassicLeagueEntry entry, int gameweek)
    {
        try
        {
            var entryPicksTask = entryClient.GetPicks(entry.Entry, gameweek);
            var entryPicks = await entryPicksTask;

            return new GameweekEntry(entry.Entry, entry.PlayerName ?? "", entry.EntryName ?? "", entryPicks);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message, e);
            return null;
        }
    }

    public async Task<GameweekEntry?> GetEntryForGameweek(GenericEntry entry, int gameweek)
    {
        try
        {
            var entryPicks = await entryClient.GetPicks(entry.Entry, gameweek);

            return new GameweekEntry(entry.Entry, entry.EntryName, entry.EntryName, entryPicks);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message, e);
            return null;
        }
    }
}
