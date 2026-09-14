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

public record ChannelSubscriptionDto(string TeamId, string ChannelId, int? LeagueId, IEnumerable<EventSubscription> Subscriptions);

public record TeamSummaryDto(string TeamId, string TeamName, IEnumerable<ChannelSubscriptionDto> Subscriptions, bool PendingRemoval);

public record BroadcastRequest(string Message);

public record UpdateChannelSubscriptionsRequest(IEnumerable<EventSubscription> Subscriptions);

public record MoveChannelRequest(string NewChannelId);

public static class AdminSlackEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/teams", GetTeams);
        group.MapGet("/teams/{teamId}", GetTeam);
        group.MapPost("/teams/{teamId}/uninstall", Uninstall);
        group.MapPost("/teams/{teamId}/channels/{channelId}/publish-standings", PublishStandings);
        group.MapPut("/teams/{teamId}/channels/{channelId}/subscriptions", UpdateChannelSubscriptions);
        group.MapPut("/teams/{teamId}/channels/{channelId}/channel", MoveChannel);
        group.MapDelete("/teams/{teamId}/channels/{channelId}", DeleteChannelSubscription);
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

        var page_ = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var items = new List<TeamSummaryDto>();
        foreach (var installation in page_)
        {
            items.Add(await ToDto(installation, teamRepo));
        }

        return TypedResults.Ok(new PagedResult<TeamSummaryDto>(items, page, pageSize, filtered.Count));
    }

    private static async Task<TeamSummaryDto> ToDto(SlackInstallation installation, ISlackTeamRepository teamRepo)
    {
        var channels = installation.ChannelSubscriptions.Select(c => ToDto(installation.TeamId, c)).ToList();
        return new(installation.TeamId, installation.TeamName, channels, installation.PendingRemoval);
    }

    private static ChannelSubscriptionDto ToDto(string teamId, SlackChannelSubscription channel) =>
        new(teamId, channel.ChannelId, channel.FollowedLeagueId is { } id ? (int)id.Value : null, ToEventSubscriptions(channel));

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

        IEnumerable<(string Id, string Name)>? slackChannels = null;
        try
        {
            var slackClient = slackClientBuilder.Build(token: installation.Token);
            var conversations = await slackClient.ConversationsListPublicChannels(500);
            slackChannels = conversations.Channels.Select(c => (c.Id, c.Name));
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }

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

            var channelStatus = slackChannels?.Any(c => channel.ChannelId == $"#{c.Name}" || channel.ChannelId == c.Id);

            channels.Add(new
            {
                channel = channel.ChannelId,
                leagueId,
                leagueName,
                subscriptions = ToEventSubscriptions(channel),
                channelStatus
            });
        }

        return TypedResults.Ok(new
        {
            teamId = installation.TeamId,
            teamName = installation.TeamName,
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
        await endpoint.Send(new PublishStandingsToSlackWorkspace(installation.TeamId, channel.ChannelId, (int)channel.FollowedLeagueId.Value, gameweek!.Id));

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

        installation.Unsubscribe(channelId, current.Except(wanted).ToArray());
        installation.Subscribe(channelId, wanted.Except(current).ToArray());

        await teamRepo.Save(installation);
        return TypedResults.Ok(new { message = $"Updated subscriptions for {channelId}" });
    }

    internal static async Task<IResult> MoveChannel(
        string teamId,
        string channelId,
        MoveChannelRequest request,
        ISlackTeamRepository teamRepo)
    {
        var teamIdToUpper = teamId.ToUpper();
        var installation = await teamRepo.FindInstallationByTeamId(teamIdToUpper);
        if (installation == null) return TypedResults.NotFound();

        if (installation.GetChannel(channelId) is null) return TypedResults.NotFound();

        installation.MoveChannel(channelId, request.NewChannelId);
        await teamRepo.DeleteChannelSubscription(teamIdToUpper, channelId);
        await teamRepo.Save(installation);

        return TypedResults.Ok(new { message = $"Moved subscription from {channelId} to {request.NewChannelId}" });
    }

    internal static async Task<IResult> DeleteChannelSubscription(
        string teamId,
        string channelId,
        ISlackTeamRepository teamRepo)
    {
        await teamRepo.DeleteChannelSubscription(teamId.ToUpper(), channelId);
        return TypedResults.Ok(new { message = $"Deleted subscription for {channelId}" });
    }

    private static FplEvent ToFplEvent(EventSubscription e) => Enum.Parse<FplEvent>(e.ToString());
}
