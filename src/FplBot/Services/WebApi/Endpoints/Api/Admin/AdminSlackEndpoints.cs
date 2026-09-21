using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Models.Mappers;
using Fpl.PulseLive;
using FplBot.ApplicationServices.Slack;
using FplBot.Data;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.EventHandlers.Slack;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.WebApi.Endpoints.Api.Admin;

// MemberCount/MemberCountUpdatedAt are Slack-only (per-channel, see conversations.info's
// num_members - ChannelMemberCountRepository) - Discord's equivalent metric lives at the guild
// level (GuildWithSubsDto), so Discord's ToDto leaves these null.
public record ChannelSubscriptionDto(
    string Id,
    string TeamId,
    string ChannelId,
    int? LeagueId,
    IEnumerable<EventSubscription> Subscriptions,
    int FailureCount,
    DateTimeOffset? FailingSince,
    string? LastFailureReason,
    int? MemberCount = null,
    DateTimeOffset? MemberCountUpdatedAt = null);

public record TeamSummaryDto(string Id, string TeamId, string TeamName, IEnumerable<ChannelSubscriptionDto> Subscriptions, bool PendingRemoval);

// TotalApproximateMembers sums each subscribed channel's own member count (see
// conversations.info's num_members) rather than a workspace-wide member count: unlike a Discord
// guild, a Slack channel isn't visible to everyone in the workspace, so per-channel is the closer
// reach proxy. This double-counts a user in multiple subscribed channels of the same workspace -
// the same kind of approximation Discord's reach number already accepts. OldestUpdate is the
// least-recently-refreshed channel currently contributing to the total.
public record TeamReachStatsDto(int TotalTeams, long TotalApproximateMembers, DateTimeOffset? OldestUpdate);

public record BroadcastRequest(string Message);

public record UpdateChannelSubscriptionsRequest(IEnumerable<EventSubscription> Subscriptions);

public record MoveChannelRequest(string NewChannelId);

public record AddChannelRequest(string ChannelId);

public record FollowLeagueRequest(int LeagueId);

public record ChannelDto(string Id, string Name);

public static class AdminSlackEndpoints
{
    private const int MaxChannelPages = 25;

    // A subscription id is enough to address a subscription, but the admin UI still needs its
    // installation to render the workspace around it.
    internal static async Task<IResult> GetSubscriptionInstallation(
        string subscriptionId,
        IIdentityResolver resolver,
        ISlackTeamRepository teamRepo)
    {
        if (await ResolveChannel(resolver, subscriptionId) is not { } resolved) return TypedResults.NotFound();

        var installation = await teamRepo.FindInstallationByTeamId(resolved.TeamId.ToUpper());
        return installation is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new { installationId = installation.Id.Value, platform = nameof(ChatPlatform.Slack) });
    }

    private static async Task<string?> ResolveTeamId(IIdentityResolver resolver, string installationId) =>
        await resolver.ResolveInstallation(new InstallationId(installationId)) is { Platform: ChatPlatform.Slack } installation
            ? installation.ExternalId
            : null;

    private static async Task<(string TeamId, string ChannelId)?> ResolveChannel(IIdentityResolver resolver, string subscriptionId) =>
        await resolver.ResolveSubscription(new SubscriptionId(subscriptionId)) is { Platform: ChatPlatform.Slack } subscription
            ? (subscription.InstallationExternalId, subscription.ChannelId)
            : null;


    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/teams", GetTeams);
        group.MapGet("/slack/reach", GetReachStats);
        group.MapPost("/slack/reach/refresh", RefreshReachStats);
        group.MapGet("/teams/{installationId}", GetTeam);
        group.MapGet("/teams/{installationId}/available-channels", GetAvailableChannels);
        group.MapPost("/teams/{installationId}/channels", AddChannel);
        group.MapPost("/teams/{installationId}/uninstall", Uninstall);
        group.MapGet("/slack/failures", GetFailureStats);
        group.MapPost("/slack/failures/reset", ResetFailures);

        group.MapPost("/slack/broadcast", BroadcastToSlack);
    }

    private static async Task<IResult> BroadcastToSlack(BroadcastRequest request, ISendEndpointProvider sendEndpointProvider, ILogger<Program> logger)
    {
        logger.LogInformation("ENQUEUEING BROADCAST TO SLACK");
        try
        {
            var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(BroadcastToSlackHandler)}"));
            await endpoint.Send(new BroadcastToSlack(request.Message));
            return TypedResults.Ok(new { message = "Slack Broadcast enqueued!" });
        }
        catch (Exception e)
        {
            return TypedResults.Ok(new { message = $"Broadcast to Slack failed '{e}'" });
        }
    }

    internal static async Task<IResult> GetFailureStats(ISlackTeamRepository repo) =>
        TypedResults.Ok(await ChannelFailureAdmin.GetStats(repo, DateTimeOffset.UtcNow));

    internal static async Task<IResult> ResetFailures(ISlackTeamRepository repo, ILogger<Program> logger)
    {
        var cleared = await ChannelFailureAdmin.ResetAll(repo, logger);
        logger.LogWarning("Admin reset delivery failure counters for {Cleared} Slack channel(s)", cleared);
        return TypedResults.Ok(new { cleared });
    }

    internal static async Task<IResult> GetTeams(string? query, int? page, int? pageSize, bool? failingOnly, int? minMembers,
        ISlackTeamRepository teamRepo, IChannelMemberCountRepository channelMemberCountRepo)
    {
        var pageNumber = page is > 0 ? page.Value : 1;
        var size = pageSize is > 0 ? Math.Min(pageSize.Value, 100) : 25;

        var installations = (await teamRepo.GetAllInstallations()).ToList();
        var memberCounts = await channelMemberCountRepo.GetAll();

        var filtered = string.IsNullOrWhiteSpace(query)
            ? installations
            :
            [
                .. installations.Where(i =>
                    i.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    i.ExternalId.Contains(query, StringComparison.OrdinalIgnoreCase))
            ];

        if (failingOnly is true)
        {
            filtered = [.. filtered.Where(i => i.ChannelSubscriptions.Any(c => c.FailureCount > 0))];
        }

        if (minMembers is > 0)
        {
            filtered = [.. filtered.Where(i => TeamMemberCount(i, memberCounts) >= minMembers)];
        }

        var page_ = filtered.Skip((pageNumber - 1) * size).Take(size).ToList();
        var items = page_.Select(installation => ToDto(installation, memberCounts)).ToList();

        return TypedResults.Ok(new PagedResult<TeamSummaryDto>(items, pageNumber, size, filtered.Count));
    }

    // A team's "size" for filtering: the sum of its subscribed channels' own member counts -
    // the same metric TeamReachStatsDto.TotalApproximateMembers sums across all teams.
    private static int TeamMemberCount(Installation installation, IReadOnlyDictionary<string, ChannelMemberCount> memberCounts) =>
        installation.ChannelSubscriptions.Sum(c => memberCounts.GetValueOrDefault(c.ChannelId)?.MemberCount ?? 0);

    // The admin analytics page's total-reach number: totalTeams is every installed workspace,
    // totalApproximateMembers sums whatever the nightly SlackChannelMemberCountChecker sweep (or
    // a manual refresh) has fetched so far - a channel not yet swept simply contributes 0.
    internal static async Task<IResult> GetReachStats(ISlackTeamRepository teamRepo, IChannelMemberCountRepository channelMemberCountRepo)
    {
        var totalTeams = (await teamRepo.GetAllInstallations()).Count();
        var counts = (await channelMemberCountRepo.GetAll()).Values;
        var totalApproximateMembers = counts.Sum(v => (long)v.MemberCount);
        var oldestUpdate = counts.Any() ? counts.Min(v => v.UpdatedAt) : (DateTimeOffset?)null;
        return TypedResults.Ok(new TeamReachStatsDto(totalTeams, totalApproximateMembers, oldestUpdate));
    }

    internal static async Task<IResult> RefreshReachStats(IPublishEndpoint publishEndpoint)
    {
        await publishEndpoint.Publish(new RefreshSlackReachStats());
        return TypedResults.Accepted("/api/admin/slack/reach");
    }

    private static TeamSummaryDto ToDto(Installation installation, IReadOnlyDictionary<string, ChannelMemberCount> memberCounts)
    {
        var channels = installation.ChannelSubscriptions.Select(c => ToDto(installation.ExternalId, c, memberCounts)).ToList();
        return new(installation.Id.Value, installation.ExternalId, installation.Name, channels, installation.PendingRemoval);
    }

    private static ChannelSubscriptionDto ToDto(string teamId, ChannelSubscription channel, IReadOnlyDictionary<string, ChannelMemberCount> memberCounts)
    {
        var count = memberCounts.GetValueOrDefault(channel.ChannelId);
        return new(channel.Id.Value, teamId, channel.ChannelId, channel.FollowedLeagueId is { } id ? (int)id.Value : null,
            ToEventSubscriptions(channel), channel.FailureCount, channel.FailingSince, channel.LastFailureReason,
            count?.MemberCount, count?.UpdatedAt);
    }

    private static IEnumerable<EventSubscription> ToEventSubscriptions(ChannelSubscription? channel) =>
        channel?.Events.Current.Select(e => Enum.Parse<EventSubscription>(e.ToString())) ?? [];

    internal static async Task<IResult> GetAvailableChannels(
        string installationId,
        IIdentityResolver resolver,
        ISlackTeamRepository teamRepo,
        ISlackClientBuilder slackClientBuilder,
        ILogger<Program> logger)
    {
        if (await ResolveTeamId(resolver, installationId) is not { } teamId) return TypedResults.NotFound();

        var installation = await teamRepo.FindInstallationByTeamId(teamId.ToUpper());
        if (installation == null) return TypedResults.NotFound();

        var slackChannels = await ListChannels(installation, slackClientBuilder, logger, MaxChannelPages);
        if (slackChannels is null)
        {
            return TypedResults.Problem(
                title: "Failed to list channels from Slack",
                detail: "The Slack conversations.list call failed. Enter a channel id manually instead.",
                statusCode: StatusCodes.Status502BadGateway);
        }

        return TypedResults.Ok(slackChannels.Select(c => new ChannelDto(c.Id, c.Name)).OrderBy(c => c.Name));
    }

    private static async Task<IReadOnlyCollection<(string Id, string Name)>?> ListChannels(
        Installation installation,
        ISlackClientBuilder slackClientBuilder,
        ILogger logger,
        int maxPages)
    {
        try
        {
            var slackClient = slackClientBuilder.Build(token: installation.Token);
            var channels = new List<(string Id, string Name)>();
            string? cursor = null;
            for (var page = 0; page < maxPages; page++)
            {
                var conversations = await slackClient.ConversationsListPublicChannels(500, cursor);
                channels.AddRange(conversations.Channels.Select(c => (c.Id, c.Name)));
                cursor = conversations.Response_Metadata?.Next_Cursor;
                if (string.IsNullOrEmpty(cursor)) break;
            }

            return channels;
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return null;
        }
    }

    internal static async Task<IResult> GetTeam(
        string installationId,
        IIdentityResolver resolver,
        ISlackTeamRepository teamRepo,
        ILeagueClient leagueClient,
        ISlackClientBuilder slackClientBuilder,
        ILogger<Program> logger)
    {
        if (await ResolveTeamId(resolver, installationId) is not { } teamId) return TypedResults.NotFound();

        var installation = await teamRepo.FindInstallationByTeamId(teamId.ToUpper());
        if (installation == null) return TypedResults.NotFound();

        var slackChannels = await ListChannels(installation, slackClientBuilder, logger, maxPages: 1);

        var channels = new List<object>();
        foreach (var channel in installation.ChannelSubscriptions)
        {
            var leagueId = channel.FollowedLeagueId?.Value;

            string? leagueName = null;
            if (leagueId.HasValue)
            {
                var league = await leagueClient.GetClassicLeague((int)leagueId.Value, tolerate404: true);
                leagueName = league?.Properties?.Name;
            }

            var match = slackChannels?.FirstOrDefault(c => channel.ChannelId == $"#{c.Name}" || channel.ChannelId == c.Id);
            var channelStatus = slackChannels is null ? (bool?)null : match?.Name is not null;
            var channelName = match?.Name;

            channels.Add(new
            {
                id = channel.Id.Value,
                channel = channel.ChannelId,
                channelName,
                leagueId,
                leagueName,
                subscriptions = ToEventSubscriptions(channel),
                channelStatus,
                failureCount = channel.FailureCount,
                failingSince = channel.FailingSince,
                lastFailureReason = channel.LastFailureReason,
                purgeEligibleAt = channel.FailingSince is { } since ? since + ChannelSubscription.MaxFailureAge : (DateTimeOffset?)null,
                failuresUntilPurge = Math.Max(0, ChannelSubscription.MaxFailures - channel.FailureCount),
                purgeFailureLimit = ChannelSubscription.MaxFailures
            });
        }

        return TypedResults.Ok(new
        {
            id = installation.Id.Value,
            teamId = installation.ExternalId,
            teamName = installation.Name,
            token = installation.Token,
            pendingRemoval = installation.PendingRemoval,
            channels
        });
    }

    internal static async Task<IResult> Uninstall(
        string installationId,
        IIdentityResolver resolver,
        AdminUninstallSlackWorkspace adminUninstallSlackWorkspace,
        ILogger<Program> logger)
    {
        if (await ResolveTeamId(resolver, installationId) is not { } teamId) return TypedResults.NotFound();

        var teamIdToUpper = teamId.ToUpper();
        logger.LogInformation("Marking {TeamId} for removal", teamIdToUpper);

        await adminUninstallSlackWorkspace.Execute(teamIdToUpper);
        return TypedResults.Accepted("/", new { message = $"Marked {teamIdToUpper} for removal." });
    }

    internal static async Task<IResult> Publish(
        string subscriptionId,
        PublishableEvent evt,
        IIdentityResolver resolver,
        ISlackTeamRepository teamRepo,
        ISendEndpointProvider sendEndpointProvider,
        IGlobalSettingsClient gameweekClient,
        IFixtureClient fixtureClient,
        ILiveClient liveClient,
        IPulseLiveClient pulseClient)
    {
        if (await ResolveChannel(resolver, subscriptionId) is not { } resolved) return TypedResults.NotFound();
        var (teamId, channelId) = resolved;

        var teamIdToUpper = teamId.ToUpper();
        var installation = await teamRepo.FindInstallationByTeamId(teamIdToUpper);
        var channel = installation?.ChannelSubscriptions.FirstOrDefault(c => c.ChannelId == channelId);
        if (installation is null || channel is null) return TypedResults.NotFound();

        var requiresLeague = evt is PublishableEvent.Standings or PublishableEvent.GameweekStarted;
        if (requiresLeague && channel.FollowedLeagueId is null)
        {
            return TypedResults.Ok(new { published = false, message = $"Did not publish. Channel {channelId} is not following a league." });
        }

        var settings = await gameweekClient.GetGlobalSettings();
        if (settings?.Gameweeks.GetCurrentGameweek() is not { } gameweek)
        {
            return TypedResults.Ok(new { published = false, message = "Could not determine the current gameweek." });
        }

        switch (evt)
        {
            case PublishableEvent.Standings:
                var standingsEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(SlackGameweekFinishedHandler)}"));
                await standingsEndpoint.Send(new PublishStandingsToSlackWorkspace(installation.ExternalId, channel.ChannelId,
                    (int)channel.FollowedLeagueId!.Value, gameweek.Id));
                break;
            case PublishableEvent.GameweekStarted:
                var startedEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(SlackGameweekStartedHandler)}"));
                await startedEndpoint.Send(new ProcessGameweekStartedForSlackChannel(installation.ExternalId, channel.ChannelId, gameweek.Id));
                break;
            case PublishableEvent.Deadline24Hours:
                var deadline24Endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(SlackNearDeadlineHandler)}"));
                await deadline24Endpoint.Send(new PublishDeadlineNotificationToSlackWorkspace(installation.ExternalId, channel.ChannelId,
                    new GameweekNearingDeadline(gameweek.Id, gameweek.Name ?? $"Gameweek {gameweek.Id}", gameweek.Deadline)));
                break;
            case PublishableEvent.Deadline1Hour:
                var deadline1Endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(PublishToSlackHandler)}"));
                await deadline1Endpoint.Send(new PublishToSlack(installation.ExternalId, channel.ChannelId,
                    $"<!channel> ⏳ Gameweek {gameweek.Id} deadline in 60 minutes!"));
                break;
            case PublishableEvent.FixtureEvents:
            {
                if (await PublishableFixtureLookup.FindFirstFixture(fixtureClient, gameweek.Id) is not { } fixture)
                {
                    return TypedResults.Ok(new { published = false, message = "No fixtures found for the current gameweek." });
                }

                var (events, refusalReason) = await PublishableFixtureLookup.ResolveFixtureEvents(fixture, gameweekClient);
                if (refusalReason is not null)
                {
                    return TypedResults.Ok(new { published = false, message = refusalReason });
                }

                var fixtureEventsEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(SlackFixtureEventsHandler)}"));
                await fixtureEventsEndpoint.Send(new PublishFixtureEventsToSlackChannel(installation.ExternalId, channel.ChannelId, events));
                break;
            }
            case PublishableEvent.FixtureFullTime:
            {
                if (await PublishableFixtureLookup.FindFirstFixture(fixtureClient, gameweek.Id) is not { } fixture)
                {
                    return TypedResults.Ok(new { published = false, message = "No fixtures found for the current gameweek." });
                }

                if (!fixture.Finished && !fixture.FinishedProvisional)
                {
                    return TypedResults.Ok(new { published = false, message = "This fixture hasn't finished yet." });
                }

                var fulltimeSettings = await gameweekClient.GetGlobalSettings();
                var liveItems = fixture.Event.HasValue ? await liveClient.GetLiveItems(fixture.Event.Value, isOngoingGameweek: true) : null;
                var finished = FixtureFulltimeModelBuilder.CreateFinishedFixture(fulltimeSettings?.Teams ?? [], fulltimeSettings?.Players ?? [], fixture, liveItems);
                var title = $"*FT: {finished.HomeTeam.ShortName} {finished.Fixture.HomeTeamScore}-{finished.Fixture.AwayTeamScore} {finished.AwayTeam.ShortName}*";
                var threadMessage = Formatter.FormatProvisionalFinished(finished);

                var fulltimeEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(SlackFixtureFulltimeHandler)}"));
                await fulltimeEndpoint.Send(new PublishFulltimeMessageToSlackWorkspace(installation.ExternalId, channel.ChannelId, title, threadMessage));
                break;
            }
            case PublishableEvent.Lineups:
            {
                if (await PublishableFixtureLookup.FindFirstFixture(fixtureClient, gameweek.Id) is not { } fixture)
                {
                    return TypedResults.Ok(new { published = false, message = "No fixtures found for the current gameweek." });
                }

                var matchDetails = await pulseClient.GetMatchDetails(fixture.Code);
                if (matchDetails is null || !matchDetails.HasLineUps())
                {
                    return TypedResults.Ok(new { published = false, message = "Lineups aren't confirmed yet for this fixture." });
                }

                var lineupSettings = await gameweekClient.GetGlobalSettings();
                var teamShortNames = lineupSettings?.Teams.ToDictionary(t => t.Id, t => t.ShortName) ?? [];
                var homeAbbr = teamShortNames.GetValueOrDefault(fixture.HomeTeamId, "?") ?? "?";
                var awayAbbr = teamShortNames.GetValueOrDefault(fixture.AwayTeamId, "?") ?? "?";
                var lineupReady = MatchDetailsMapper.TryMapToLineup(matchDetails, fixture.Code, homeAbbr, awayAbbr);
                if (lineupReady is null)
                {
                    return TypedResults.Ok(new { published = false, message = "Could not map lineups for this fixture." });
                }

                var lineupsEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(SlackLineupReadyHandler)}"));
                await lineupsEndpoint.Send(new PublishLineupsToSlackWorkspace(installation.ExternalId, channel.ChannelId, lineupReady.Lineup));
                break;
            }
        }

        return TypedResults.Ok(new { published = true, message = $"Published {evt} to {channelId}" });
    }

    internal static async Task<IResult> UpdateChannelSubscriptions(
        string subscriptionId,
        UpdateChannelSubscriptionsRequest request,
        IIdentityResolver resolver,
        ISlackTeamRepository teamRepo)
    {
        if (await ResolveChannel(resolver, subscriptionId) is not { } resolved) return TypedResults.NotFound();
        var (teamId, channelId) = resolved;

        var teamIdToUpper = teamId.ToUpper();
        var installation = await teamRepo.FindInstallationByTeamId(teamIdToUpper);
        if (installation == null) return TypedResults.NotFound();

        var wanted = request.Subscriptions.Select(ToFplEvent).ToHashSet();
        var current = installation.GetChannel(channelId)?.Events.Current.ToHashSet() ?? [];

        installation.Unsubscribe(channelId, [.. current.Except(wanted)]);
        installation.Subscribe(channelId, [.. wanted.Except(current)]);

        await teamRepo.Save(installation);
        return TypedResults.Ok(new { message = $"Updated subscriptions for {channelId}" });
    }

    internal static async Task<IResult> FollowLeague(
        string subscriptionId,
        FollowLeagueRequest request,
        IIdentityResolver resolver,
        ISlackTeamRepository teamRepo,
        ILeagueClient leagueClient)
    {
        if (await ResolveChannel(resolver, subscriptionId) is not { } resolved) return TypedResults.NotFound();
        var (teamId, channelId) = resolved;

        var installation = await teamRepo.FindInstallationByTeamId(teamId.ToUpper());
        if (installation == null) return TypedResults.NotFound();
        if (installation.GetChannel(channelId) is null) return TypedResults.NotFound();

        var league = await leagueClient.GetClassicLeague(request.LeagueId, tolerate404: true);
        if (league == null)
        {
            return TypedResults.BadRequest(new { message = $"Could not find a classic league with id '{request.LeagueId}'." });
        }

        installation.Follow(channelId, new ClassicLeagueId(request.LeagueId));
        await teamRepo.Save(installation);

        var leagueName = league.Properties?.Name;
        return TypedResults.Ok(new { message = $"{channelId} now follows '{leagueName}' ({request.LeagueId})", leagueName });
    }

    internal static async Task<IResult> UnfollowLeague(
        string subscriptionId,
        IIdentityResolver resolver,
        ISlackTeamRepository teamRepo)
    {
        if (await ResolveChannel(resolver, subscriptionId) is not { } resolved) return TypedResults.NotFound();
        var (teamId, channelId) = resolved;

        var installation = await teamRepo.FindInstallationByTeamId(teamId.ToUpper());
        if (installation == null) return TypedResults.NotFound();
        if (installation.GetChannel(channelId) is null) return TypedResults.NotFound();

        installation.Unfollow(channelId);
        await teamRepo.Save(installation);

        return TypedResults.Ok(new { message = $"{channelId} no longer follows a league" });
    }

    internal static async Task<IResult> AddChannel(
        string installationId,
        AddChannelRequest request,
        IIdentityResolver resolver,
        ISlackTeamRepository teamRepo)
    {
        if (await ResolveTeamId(resolver, installationId) is not { } teamId) return TypedResults.NotFound();

        var installation = await teamRepo.FindInstallationByTeamId(teamId.ToUpper());
        if (installation == null) return TypedResults.NotFound();

        if (string.IsNullOrWhiteSpace(request.ChannelId))
        {
            return TypedResults.BadRequest(new { message = "A channel id is required." });
        }

        if (installation.GetChannel(request.ChannelId) is not null)
        {
            return TypedResults.Conflict(new { message = $"{request.ChannelId} already has a subscription." });
        }

        installation.Subscribe(request.ChannelId, [FplEvent.All]);
        await teamRepo.Save(installation);

        return TypedResults.Ok(new { message = $"Subscribed {request.ChannelId} to all events" });
    }

    internal static async Task<IResult> MoveChannel(
        string subscriptionId,
        MoveChannelRequest request,
        IIdentityResolver resolver,
        ISlackTeamRepository teamRepo,
        IPublishEndpoint publishEndpoint)
    {
        if (await ResolveChannel(resolver, subscriptionId) is not { } resolved) return TypedResults.NotFound();
        var (teamId, channelId) = resolved;

        var teamIdToUpper = teamId.ToUpper();
        var installation = await teamRepo.FindInstallationByTeamId(teamIdToUpper);
        if (installation == null) return TypedResults.NotFound();

        var moveChannelOutcome = installation.MoveChannel(channelId, request.NewChannelId);
        IResult result = moveChannelOutcome switch
        {
            MoveChannelOutcome.SourceNotFound => TypedResults.NotFound(),
            MoveChannelOutcome.TargetAlreadySubscribed => TypedResults.Conflict(new { message = $"{request.NewChannelId} already has a subscription." }),
            MoveChannelOutcome.Moved => TypedResults.Ok(new { message = $"Moved subscription from {channelId} to {request.NewChannelId}" }),
            _ => throw new ArgumentOutOfRangeException(nameof(moveChannelOutcome), moveChannelOutcome, null)
        };

        if (moveChannelOutcome is not MoveChannelOutcome.Moved)
        {
            return result;
        }

        await teamRepo.Save(installation);

        await publishEndpoint.Publish(new SlackChannelMoved(teamIdToUpper, channelId, request.NewChannelId));

        return result;
    }

    internal static async Task<IResult> DeleteChannelSubscription(
        string subscriptionId,
        IIdentityResolver resolver,
        ISlackTeamRepository teamRepo)
    {
        if (await ResolveChannel(resolver, subscriptionId) is not { } resolved) return TypedResults.NotFound();
        var (teamId, channelId) = resolved;

        var installation = await teamRepo.FindInstallationByTeamId(teamId.ToUpper());
        if (installation == null) return TypedResults.NotFound();

        installation.RemoveChannel(channelId);
        await teamRepo.Save(installation);

        return TypedResults.Ok(new { message = $"Deleted subscription for {channelId}" });
    }

    private static FplEvent ToFplEvent(EventSubscription e) => Enum.Parse<FplEvent>(e.ToString());
}
