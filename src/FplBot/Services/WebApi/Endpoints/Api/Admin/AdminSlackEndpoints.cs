using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.ApplicationServices.Slack;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.EventHandlers.Slack;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.WebApi.Endpoints.Api.Admin;

public record ChannelSubscriptionDto(string TeamId, string ChannelId, int? LeagueId, IEnumerable<EventSubscription> Subscriptions, int FailureCount, DateTimeOffset? FailingSince, string? LastFailureReason);

public record TeamSummaryDto(string TeamId, string TeamName, IEnumerable<ChannelSubscriptionDto> Subscriptions, bool PendingRemoval);

public record BroadcastRequest(string Message);

public record UpdateChannelSubscriptionsRequest(IEnumerable<EventSubscription> Subscriptions);

public record MoveChannelRequest(string NewChannelId);

public record AddChannelRequest(string ChannelId);

public record FollowLeagueRequest(int LeagueId);

public record ChannelDto(string Id, string Name);

public static class AdminSlackEndpoints
{
    private const int MaxChannelPages = 25;

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/teams", GetTeams);
        group.MapGet("/teams/{teamId}", GetTeam);
        group.MapGet("/teams/{teamId}/available-channels", GetAvailableChannels);
        group.MapPost("/teams/{teamId}/channels", AddChannel);
        group.MapPost("/teams/{teamId}/uninstall", Uninstall);
        group.MapPost("/teams/{teamId}/channels/{channelId}/publish-standings", PublishStandings);
        group.MapPut("/teams/{teamId}/channels/{channelId}/subscriptions", UpdateChannelSubscriptions);
        group.MapPut("/teams/{teamId}/channels/{channelId}/channel", MoveChannel);
        group.MapPut("/teams/{teamId}/channels/{channelId}/league", FollowLeague);
        group.MapDelete("/teams/{teamId}/channels/{channelId}/league", UnfollowLeague);
        group.MapDelete("/teams/{teamId}/channels/{channelId}", DeleteChannelSubscription);
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

    internal static async Task<IResult> GetTeams(string? query, int page, int pageSize, bool failingOnly, ISlackTeamRepository teamRepo)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 25 : Math.Min(pageSize, 100);

        var installations = (await teamRepo.GetAllInstallations()).ToList();

        var filtered = string.IsNullOrWhiteSpace(query)
            ? installations
            : [.. installations.Where(i =>
                i.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                i.Id.Contains(query, StringComparison.OrdinalIgnoreCase))];

        if (failingOnly)
        {
            filtered = [.. filtered.Where(i => i.ChannelSubscriptions.Any(c => c.FailureCount > 0))];
        }

        var page_ = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var items = new List<TeamSummaryDto>();
        foreach (var installation in page_)
        {
            items.Add(await ToDto(installation, teamRepo));
        }

        return TypedResults.Ok(new PagedResult<TeamSummaryDto>(items, page, pageSize, filtered.Count));
    }

    private static async Task<TeamSummaryDto> ToDto(Installation installation, ISlackTeamRepository teamRepo)
    {
        var channels = installation.ChannelSubscriptions.Select(c => ToDto(installation.Id, c)).ToList();
        return new(installation.Id, installation.Name, channels, installation.PendingRemoval);
    }

    private static ChannelSubscriptionDto ToDto(string teamId, ChannelSubscription channel) =>
        new(teamId, channel.ChannelId, channel.FollowedLeagueId is { } id ? (int)id.Value : null,
            ToEventSubscriptions(channel), channel.FailureCount, channel.FailingSince, channel.LastFailureReason);

    private static IEnumerable<EventSubscription> ToEventSubscriptions(ChannelSubscription? channel) =>
        channel?.Events.Current.Select(e => Enum.Parse<EventSubscription>(e.ToString())) ?? [];

    internal static async Task<IResult> GetAvailableChannels(
        string teamId,
        ISlackTeamRepository teamRepo,
        ISlackClientBuilder slackClientBuilder,
        ILogger<Program> logger)
    {
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
        string teamId,
        ISlackTeamRepository teamRepo,
        ILeagueClient leagueClient,
        ISlackClientBuilder slackClientBuilder,
        ILogger<Program> logger)
    {
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
            teamId = installation.Id,
            teamName = installation.Name,
            token = installation.Token,
            pendingRemoval = installation.PendingRemoval,
            channels
        });
    }

    internal static async Task<IResult> Uninstall(
        string teamId,
        AdminUninstallSlackWorkspace adminUninstallSlackWorkspace,
        ILogger<Program> logger)
    {
        var teamIdToUpper = teamId.ToUpper();
        logger.LogInformation("Marking {TeamId} for removal", teamIdToUpper);

        await adminUninstallSlackWorkspace.Execute(teamIdToUpper);
        return TypedResults.Accepted("/", new { message = $"Marked {teamIdToUpper} for removal." });
    }

    internal static async Task<IResult> PublishStandings(
        string teamId,
        string channelId,
        ISlackTeamRepository teamRepo,
        ISendEndpointProvider sendEndpointProvider,
        IGlobalSettingsClient gameweekClient)
    {
        var teamIdToUpper = teamId.ToUpper();
        var installation = await teamRepo.FindInstallationByTeamId(teamIdToUpper);
        if (installation == null) return TypedResults.NotFound();

        var channels = installation.ChannelSubscriptions;
        var channel = channels.FirstOrDefault(c => c.ChannelId == channelId);
        if (channel?.FollowedLeagueId is null)
        {
            return TypedResults.Ok(new { published = false, message = $"Did not publish. Channel {channelId} is not following a league." });
        }

        var settings = await gameweekClient.GetGlobalSettings();
        var gameweek = settings!.Gameweeks.GetCurrentGameweek();

        var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(SlackGameweekFinishedHandler)}"));
        await endpoint.Send(new PublishStandingsToSlackWorkspace(installation.Id, channel.ChannelId, (int)channel.FollowedLeagueId.Value, gameweek!.Id));

        return TypedResults.Ok(new { published = true, message = $"Published standings to {channelId}" });
    }

    internal static async Task<IResult> UpdateChannelSubscriptions(
        string teamId,
        string channelId,
        UpdateChannelSubscriptionsRequest request,
        ISlackTeamRepository teamRepo)
    {
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
        string teamId,
        string channelId,
        FollowLeagueRequest request,
        ISlackTeamRepository teamRepo,
        ILeagueClient leagueClient)
    {
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
        string teamId,
        string channelId,
        ISlackTeamRepository teamRepo)
    {
        var installation = await teamRepo.FindInstallationByTeamId(teamId.ToUpper());
        if (installation == null) return TypedResults.NotFound();
        if (installation.GetChannel(channelId) is null) return TypedResults.NotFound();

        installation.Unfollow(channelId);
        await teamRepo.Save(installation);

        return TypedResults.Ok(new { message = $"{channelId} no longer follows a league" });
    }

    internal static async Task<IResult> AddChannel(
        string teamId,
        AddChannelRequest request,
        ISlackTeamRepository teamRepo)
    {
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
        string teamId,
        string channelId,
        MoveChannelRequest request,
        ISlackTeamRepository teamRepo,
        IPublishEndpoint publishEndpoint)
    {
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
        string teamId,
        string channelId,
        ISlackTeamRepository teamRepo)
    {
        var installation = await teamRepo.FindInstallationByTeamId(teamId.ToUpper());
        if (installation == null) return TypedResults.NotFound();

        installation.RemoveChannel(channelId);
        await teamRepo.Save(installation);

        return TypedResults.Ok(new { message = $"Deleted subscription for {channelId}" });
    }

    private static FplEvent ToFplEvent(EventSubscription e) => Enum.Parse<FplEvent>(e.ToString());
}
