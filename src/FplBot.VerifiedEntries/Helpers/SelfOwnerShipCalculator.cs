using Fpl.Client.Abstractions;
using Fpl.Client.Models;

namespace FplBot.VerifiedEntries.Helpers;

public class SelfOwnerShipCalculator
{
    private readonly IEntryClient _entryClient;

    public SelfOwnerShipCalculator(IEntryClient entryClient)
    {
        _entryClient = entryClient;
    }

    public async Task<IEnumerable<int>> CalculateSelfOwnershipPoints(int entryId, int? plPlayerId, IEnumerable<int> gameweeks, ICollection<LiveItem>[] liveItems)
    {
        var allPicks = await Task.WhenAll(gameweeks.Select(gw => GetPick(entryId, gw)));

        var selfPicks = allPicks
            .Where(p => p.Pick != null)
            .Select(p => (p.Gameweek, SelfPick: p.Pick.Picks.SingleOrDefault(pick => pick.PlayerId == plPlayerId)))
            .Where(x => x.SelfPick != null)
            .ToArray();

        if (!selfPicks.Any())
        {
            return Enumerable.Empty<int>();
        }

        var gwPointsForSelfPick = selfPicks.Select(s => GetPickScore(liveItems[s.Gameweek - 1], s.SelfPick.PlayerId, s.SelfPick.Multiplier));

        return gwPointsForSelfPick;
    }

    private async Task<GameweekPick> GetPick(int entryId, int gw)
    {
        var picks = await _entryClient.GetPicks(entryId, gw, tolerate404: true);
        return new GameweekPick(gw, picks);
    }

    public int GetPickScore(ICollection<LiveItem> liveItems, int playerId, int multiplier)
    {
        return (liveItems.SingleOrDefault(x => x.Id == playerId)?.Stats?.TotalPoints ?? 0) * multiplier;
    }
}

public class GameweekPick
{
    public int Gameweek { get; }
    public EntryPicks Pick { get; }

    public GameweekPick(int gameweek, EntryPicks pick)
    {
        Gameweek = gameweek;
        Pick = pick;
    }
}
