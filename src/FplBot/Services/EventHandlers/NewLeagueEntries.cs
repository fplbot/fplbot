using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data;
using FplBot.Domain;

namespace FplBot.EventHandlers;

public static class NewLeagueEntries
{
    public record ChannelEntries(string InstallationId, string ChannelId, string LeagueName, IReadOnlyList<NewLeagueEntry> Entries, bool HasMore);

    public static async Task<IReadOnlyList<ChannelEntries>> ResolveForSubscribedChannels(
        IDomainRepository repository,
        ILeagueClient leagueClient,
        int gameweekId,
        ILogger logger)
    {
        var resolved = new List<ChannelEntries>();
        var fetchedLeagues = new Dictionary<int, (string Name, IReadOnlyList<NewLeagueEntry> Entries, bool HasMore)?>();

        foreach (var (installationId, channelId) in await repository.GetChannelsSubscribedTo(FplEvent.NewLeagueEntries))
        {
            var subscription = await repository.GetChannelSubscription(installationId, channelId);
            if (subscription?.FollowedLeagueId is not { } followedLeagueId)
            {
                continue;
            }

            var leagueId = (int)followedLeagueId.Value;
            if (!fetchedLeagues.TryGetValue(leagueId, out var league))
            {
                league = await Fetch(leagueClient, leagueId, gameweekId, logger);
                fetchedLeagues[leagueId] = league;
            }

            if (league is not { } found || found.Entries.Count == 0)
            {
                continue;
            }

            resolved.Add(new ChannelEntries(installationId, channelId, found.Name, found.Entries, found.HasMore));
        }

        return resolved;
    }

    private static async Task<(string Name, IReadOnlyList<NewLeagueEntry> Entries, bool HasMore)?> Fetch(
        ILeagueClient leagueClient,
        int leagueId,
        int gameweekId,
        ILogger logger)
    {
        try
        {
            var league = await leagueClient.GetClassicLeague(leagueId, tolerate404: true);
            if (league is null)
            {
                return null;
            }

            if (gameweekId <= (league.Properties?.StartEvent ?? 1))
            {
                logger.LogInformation("Skipping new entries for league {LeagueId}: gameweek {GameweekId} is the league's first", leagueId, gameweekId);
                return null;
            }

            var newEntries = league.NewEntries;
            return (league.Properties?.Name ?? $"league {leagueId}",
                    (newEntries?.Entries ?? []).ToList(),
                    newEntries?.HasNext ?? false);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Could not fetch new entries for league {LeagueId}", leagueId);
            return null;
        }
    }
}
