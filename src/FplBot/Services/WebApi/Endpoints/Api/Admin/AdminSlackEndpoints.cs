using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.ApplicationServices.Slack;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.EventHandlers.Slack;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.WebApi.Endpoints.Api.Admin;

public record TeamSummaryDto(string TeamId, string? TeamName, string? Channel, int? LeagueId, IEnumerable<EventSubscription> Subscriptions, bool PendingRemoval);

public record UpdateTeamRequest(int LeagueId, string Channel, EventSubscription[] Subscriptions);

public record PublishEventRequest(EventSubscription[] Subscriptions);

public record BroadcastRequest(string Message);

public static class AdminSlackEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/teams", GetTeams);
        group.MapGet("/teams/{teamId}", GetTeam);
        group.MapPost("/teams/{teamId}/uninstall", Uninstall);
        group.MapPut("/teams/{teamId}", UpdateTeam);
        group.MapPost("/teams/{teamId}/publish-event", PublishTeamEvent);
        group.MapPost("/slack/broadcast", BroadcastToSlack);
    }

    private static async Task<IResult> BroadcastToSlack(BroadcastRequest request, ISendEndpointProvider sendEndpointProvider, ILogger<Program> logger)
    {
        logger.LogInformation("ENQUEUEING BROADCAST TO SLACK");
        try
        {
            var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(BroadcastToSlackHandler)}"));
            await endpoint.Send(new FplBot.Messaging.Contracts.Commands.v1.BroadcastToSlack(request.Message));
            return TypedResults.Ok(new { message = "Slack Broadcast enqueued!" });
        }
        catch (Exception e)
        {
            return TypedResults.Ok(new { message = $"Broadcast to Slack failed '{e}'" });
        }
    }

    internal static async Task<IResult> GetTeams(string? query, int page, int pageSize, ISlackTeamRepository teamRepo)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 25 : Math.Min(pageSize, 100);

        var installations = (await teamRepo.GetAllInstallations()).ToList();

        var filtered = string.IsNullOrWhiteSpace(query)
            ? installations
            : installations.Where(i =>
                i.TeamName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                i.TeamId.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToList();

        return TypedResults.Ok(new PagedResult<TeamSummaryDto>(items, page, pageSize, filtered.Count));
    }

    private static TeamSummaryDto ToDto(SlackInstallation installation)
    {
        var channel = installation.ChannelSubscriptions.FirstOrDefault();
        var leagueId = channel?.FollowedLeagueId?.Value;
        return new(installation.TeamId, installation.TeamName, channel?.ChannelId, leagueId.HasValue ? (int)leagueId.Value : null, ToEventSubscriptions(channel), installation.PendingRemoval);
    }

    private static IEnumerable<EventSubscription> ToEventSubscriptions(SlackChannelSubscription? channel) =>
        channel?.Events.Current.Select(e => Enum.Parse<EventSubscription>(e.ToString())) ?? [];

    private static async Task<IResult> GetTeam(
        string teamId,
        ISlackTeamRepository teamRepo,
        ILeagueClient leagueClient,
        ISlackClientBuilder slackClientBuilder,
        ILogger<Program> logger)
    {
        var installation = await teamRepo.FindInstallationByTeamId(teamId.ToUpper());
        if (installation == null) return TypedResults.NotFound();

        var channel = installation.ChannelSubscriptions.FirstOrDefault();
        var leagueId = channel?.FollowedLeagueId?.Value;

        string? leagueName = null;
        if (leagueId.HasValue)
        {
            var league = await leagueClient.GetClassicLeague((int)leagueId.Value, tolerate404: true);
            leagueName = league?.Properties?.Name;
        }

        bool? channelStatus = null;
        try
        {
            var slackClient = slackClientBuilder.Build(token: installation.Token);
            var channels = await slackClient.ConversationsListPublicChannels(500);
            channelStatus = channels.Channels.Any(c => channel?.ChannelId == $"#{c.Name}" || channel?.ChannelId == c.Id);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }

        return TypedResults.Ok(new
        {
            teamId = installation.TeamId,
            teamName = installation.TeamName,
            channel = channel?.ChannelId,
            leagueId,
            leagueName,
            subscriptions = ToEventSubscriptions(channel),
            channelStatus,
            pendingRemoval = installation.PendingRemoval
        });
    }

    private static async Task<IResult> Uninstall(
        string teamId,
        AdminUninstallSlackWorkspace adminUninstallSlackWorkspace,
        ILogger<Program> logger)
    {
        var teamIdToUpper = teamId.ToUpper();
        logger.LogInformation("Marking {TeamId} for removal", teamIdToUpper);

        await adminUninstallSlackWorkspace.Execute(teamIdToUpper);
        return TypedResults.Accepted("/", new { message = $"Marked {teamIdToUpper} for removal." });
    }

    private static async Task<IResult> UpdateTeam(
        string teamId,
        UpdateTeamRequest request,
        ISlackTeamRepository teamRepo,
        ILeagueClient leagueClient,
        ISlackClientBuilder slackClientBuilder,
        ILogger<Program> logger)
    {
        var teamIdToUpper = teamId.ToUpper();
        var installation = await teamRepo.FindInstallationByTeamId(teamIdToUpper);
        if (installation == null) return TypedResults.NotFound();

        var warnings = new List<string>();

        var league = await leagueClient.GetClassicLeague(request.LeagueId, tolerate404: true);
        if (league == null)
        {
            warnings.Add("League does not exist.");
        }

        // A Slack API failure here (e.g. a dev-seeded team's fake token) is a warning, not
        // a hard failure — same tolerance GetTeam already has for this exact call, and
        // consistent with every other check in this handler: report it and still save.
        try
        {
            var slackClient = slackClientBuilder.Build(token: installation.Token);
            var channelsRes = await slackClient.ConversationsListPublicChannels(500);
            var channelFound = channelsRes.Channels.Any(c => request.Channel == $"#{c.Name}" || request.Channel == c.Id);
            if (!channelFound)
            {
                var channelsText = string.Join(',', channelsRes.Channels.Select(c => c.Name));
                warnings.Add($"Could not find channel via Slack API lookup. Channels: {channelsText}");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to verify channel {Channel} for team {TeamId} via Slack API", request.Channel, teamIdToUpper);
            warnings.Add($"Could not verify channel via Slack API: {e.Message}");
        }

        await teamRepo.UpdateLeagueId(teamIdToUpper, request.LeagueId);
        await teamRepo.UpdateChannel(teamIdToUpper, request.Channel);
        await teamRepo.UpdateSubscriptions(teamIdToUpper, request.Subscriptions);

        return TypedResults.Ok(new { updated = true, warnings });
    }

    internal static async Task<IResult> PublishTeamEvent(
        string teamId,
        PublishEventRequest request,
        ISlackTeamRepository teamRepo,
        ISendEndpointProvider sendEndpointProvider,
        IGlobalSettingsClient gameweekClient)
    {
        if (request.Subscriptions.Length == 0)
        {
            return TypedResults.Ok(new { published = false, message = "No subscriptions selected." });
        }

        var teamIdToUpper = teamId.ToUpper();
        var installation = await teamRepo.FindInstallationByTeamId(teamIdToUpper);
        if (installation == null) return TypedResults.NotFound();

        if (!request.Subscriptions.Contains(EventSubscription.Standings))
        {
            return TypedResults.Ok(new { published = false, message = "Unsupported event. Nothing published." });
        }

        var settings = await gameweekClient.GetGlobalSettings();
        var gameweek = settings!.Gameweeks.GetCurrentGameweek();

        var channel = installation.ChannelSubscriptions.FirstOrDefault();
        if (channel?.FollowedLeagueId is null || string.IsNullOrEmpty(channel.ChannelId))
        {
            return TypedResults.Ok(new { published = false, message = $"Did not publish. Missing fpl league id for {teamIdToUpper}" });
        }

        var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(SlackGameweekFinishedHandler)}"));
        await endpoint.Send(new PublishStandingsToSlackWorkspace(installation.TeamId, channel.ChannelId, (int)channel.FollowedLeagueId.Value, gameweek!.Id));

        return TypedResults.Ok(new { published = true, message = $"Published standings to {teamIdToUpper}" });
    }
}
