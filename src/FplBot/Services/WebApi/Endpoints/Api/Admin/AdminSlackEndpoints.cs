using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Slackbot.Net.Endpoints.Hosting;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Exceptions;

namespace FplBot.WebApi.Endpoints.Api.Admin;

public record TeamSummaryDto(string TeamId, string? TeamName, string? Channel, int? LeagueId, IEnumerable<EventSubscription> Subscriptions);

public record UpdateTeamRequest(int LeagueId, string Channel, EventSubscription[] Subscriptions);

public record PublishEventRequest(EventSubscription[] Subscriptions);

public record BroadcastRequest(string Message);

public static class AdminSlackEndpoints
{
    private const string TeamsCacheKey = "admin:teams";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

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

    internal static async Task<IResult> GetTeams(string? query, int page, int pageSize, ISlackTeamRepository teamRepo, IMemoryCache cache)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 25 : Math.Min(pageSize, 100);

        var teams = (await cache.GetOrCreateAsync(TeamsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return (await teamRepo.GetAllTeams()).ToList();
        }))!;

        var filtered = string.IsNullOrWhiteSpace(query)
            ? teams
            : teams.Where(t =>
                (t.TeamName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.TeamId?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToList();

        return TypedResults.Ok(new PagedResult<TeamSummaryDto>(items, page, pageSize, filtered.Count));
    }

    private static TeamSummaryDto ToDto(SlackTeam t) =>
        new(t.TeamId ?? "", t.TeamName, t.FplBotSlackChannel, t.FplbotLeagueId, t.Subscriptions);

    private static async Task<IResult> GetTeam(
        string teamId,
        ISlackTeamRepository teamRepo,
        ILeagueClient leagueClient,
        ISlackClientBuilder slackClientBuilder,
        ILogger<Program> logger)
    {
        var team = await teamRepo.GetTeam(teamId.ToUpper());
        if (team == null) return TypedResults.NotFound();

        string? leagueName = null;
        if (team.FplbotLeagueId.HasValue)
        {
            var league = await leagueClient.GetClassicLeague(team.FplbotLeagueId.Value, tolerate404: true);
            leagueName = league?.Properties?.Name;
        }

        bool? channelStatus = null;
        try
        {
            var slackClient = slackClientBuilder.Build(token: team.AccessToken);
            var channels = await slackClient.ConversationsListPublicChannels(500);
            channelStatus = channels.Channels.Any(c => team.FplBotSlackChannel == $"#{c.Name}" || team.FplBotSlackChannel == c.Id);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }

        return TypedResults.Ok(new
        {
            teamId = team.TeamId,
            teamName = team.TeamName,
            channel = team.FplBotSlackChannel,
            leagueId = team.FplbotLeagueId,
            leagueName,
            subscriptions = team.Subscriptions,
            channelStatus
        });
    }

    private static async Task<IResult> Uninstall(
        string teamId,
        ISlackTeamRepository teamRepo,
        ISlackClientBuilder slackClientBuilder,
        IOptions<OAuthOptions> slackAppOptions,
        ILogger<Program> logger)
    {
        var teamIdToUpper = teamId.ToUpper();
        logger.LogInformation("Deleting {TeamId}", teamIdToUpper);

        var team = await teamRepo.GetTeam(teamIdToUpper);
        if (team == null) return TypedResults.NotFound();

        var slackClient = slackClientBuilder.Build(token: team.AccessToken);
        try
        {
            var res = await slackClient.AppsUninstall(slackAppOptions.Value.CLIENT_ID, slackAppOptions.Value.CLIENT_SECRET);
            return res.Ok
                ? TypedResults.Ok(new { message = "Uninstall queued, and will be handled at some point" })
                : TypedResults.Ok(new { message = $"Uninstall failed '{res.Error}'" });
        }
        catch (WellKnownSlackApiException e) when (e.Message is "account_inactive" or "not_authed")
        {
            await teamRepo.DeleteByTeamId(teamIdToUpper);
            return TypedResults.Ok(new { message = "Token no longer valid. Team deleted." });
        }
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
        var team = await teamRepo.GetTeam(teamIdToUpper);
        if (team == null) return TypedResults.NotFound();

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
            var slackClient = slackClientBuilder.Build(token: team.AccessToken);
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
        var team = await teamRepo.GetTeam(teamIdToUpper);
        if (team == null) return TypedResults.NotFound();

        if (!request.Subscriptions.Contains(EventSubscription.Standings))
        {
            return TypedResults.Ok(new { published = false, message = "Unsupported event. Nothing published." });
        }

        var settings = await gameweekClient.GetGlobalSettings();
        var gameweek = settings!.Gameweeks.GetCurrentGameweek();

        if (!team.FplbotLeagueId.HasValue || string.IsNullOrEmpty(team.FplBotSlackChannel))
        {
            return TypedResults.Ok(new { published = false, message = $"Did not publish. Missing fpl league id for {teamIdToUpper}" });
        }

        var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(SlackGameweekFinishedHandler)}"));
        await endpoint.Send(new PublishStandingsToSlackWorkspace(team.TeamId ?? "", team.FplBotSlackChannel ?? "", team.FplbotLeagueId.Value, gameweek!.Id));

        return TypedResults.Ok(new { published = true, message = $"Published standings to {teamIdToUpper}" });
    }
}
