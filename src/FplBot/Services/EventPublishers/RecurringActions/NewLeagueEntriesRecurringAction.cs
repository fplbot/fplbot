using System.Net;
using CronBackgroundServices;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Extensions;
using Fpl.EventPublishers.Helpers;
using FplBot.Data;
using FplBot.Data.Discord;
using FplBot.Data.Leagues;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Hosting;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace Fpl.EventPublishers.RecurringActions;

/// <summary>
///     Polls the classic leagues followed by channels subscribing to <see cref="FplEvent.NewLeagueEntries" />
///     and publishes the entries that joined since the previous poll. The join time of the most recent
///     entry seen per league is bookmarked in Redis, so restarts don't re-notify about old entries.
/// </summary>
public class NewLeagueEntriesRecurringAction(
    ILeagueClient leagueClient,
    ISlackTeamRepository slackTeamRepo,
    IGuildRepository guildRepo,
    ILeagueEntriesBookmarkProvider bookmarkProvider,
    IServiceScopeFactory scopeFactory,
    ILogger<NewLeagueEntriesRecurringAction> logger)
    : IRecurringAction
{
    public async Task Process(CancellationToken stoppingToken)
    {
        using var activity = FplBotDiagnostics.ActivitySource.StartActivity(nameof(NewLeagueEntriesRecurringAction));
        using var scope = logger.BeginCorrelationScope();
        try
        {
            await PublishIfNewEntries();
        }
        catch (Exception e) when (LogError(e))
        {
        }
    }

    private async Task PublishIfNewEntries()
    {
        foreach (var leagueId in await GetFollowedLeagueIds())
        {
            var league = await leagueClient.GetClassicLeague(leagueId, tolerate404: true);
            var entries = league?.NewEntries?.Entries ?? [];
            var lastSeenJoinTime = await bookmarkProvider.GetLastSeenJoinTime(leagueId);

            if (lastSeenJoinTime is null)
            {
                // First time this league is polled: bookmark whatever has already joined (or, when
                // nothing is pending, the time tracking started), so that the channel only gets
                // notified about entries joining from now on.
                logger.LogInformation("Init state for league {leagueId}", leagueId);
                await bookmarkProvider.SetLastSeenJoinTime(leagueId,
                    entries.Any() ? entries.Max(e => e.JoinedAt) : DateTime.UtcNow);
                continue;
            }

            var newEntries = entries.Where(e => e.JoinedAt > lastSeenJoinTime).OrderBy(e => e.JoinedAt).ToList();
            if (!newEntries.Any())
            {
                continue;
            }

            await bookmarkProvider.SetLastSeenJoinTime(leagueId, newEntries.Max(e => e.JoinedAt));

            logger.LogInformation("Publishing {count} new entries in league {leagueId}", newEntries.Count, leagueId);
            using var scope = scopeFactory.CreateScope();
            var publish = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            await publish.Publish(new NewLeagueEntriesRegistered(
                leagueId,
                league?.Properties?.Name ?? $"league {leagueId}",
                newEntries.Select(ToEntrant).ToList()));
        }
    }

    private static NewLeagueEntrant ToEntrant(NewLeagueEntry entry) =>
        new(entry.Entry,
            entry.EntryName ?? string.Empty,
            $"{entry.PlayerFirstName} {entry.PlayerLastName}".Trim());

    private async Task<IEnumerable<int>> GetFollowedLeagueIds()
    {
        var leagueIds = new HashSet<int>();
        foreach (var repo in new IDomainRepository[] { slackTeamRepo, guildRepo })
        {
            foreach (var (installationId, channelId) in await repo.GetChannelsSubscribedTo(FplEvent.NewLeagueEntries))
            {
                var channel = await repo.GetChannelSubscription(installationId, channelId);
                if (channel?.FollowedLeagueId is { } followedLeagueId)
                {
                    leagueIds.Add((int)followedLeagueId.Value);
                }
            }
        }

        return leagueIds;
    }

    private bool LogError(Exception e)
    {
        if (e is HttpRequestException { StatusCode: HttpStatusCode.ServiceUnavailable })
        {
            logger.LogWarning("Game is updating");
        }
        else
        {
            logger.LogError(e, e.Message);
        }

        return true;
    }

    public string Cron => CronPatterns.EveryFiveMinutesAt40seconds;
}
