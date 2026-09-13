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

public static class AdminSlackEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/teams", GetTeams);
        group.MapGet("/teams/legacy", GetLegacyTeams);
        group.MapGet("/teams/{teamId}", GetTeam);
        group.MapPost("/teams/{teamId}/uninstall", Uninstall);
        group.MapPost("/teams/{teamId}/migrate-to-v2", MigrateToV2);
        group.MapPost("/teams/{teamId}/channels/{channelId}/publish-standings", PublishStandings);
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

        var page_ = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var items = new List<TeamSummaryDto>();
        foreach (var installation in page_)
        {
            items.Add(await ToDto(installation, teamRepo));
        }

        return TypedResults.Ok(new PagedResult<TeamSummaryDto>(items, page, pageSize, filtered.Count));
    }

    // A team not yet migrated to V2 (no SlackChannelSubscriptionRecords yet) but with V1
    // subscriptions still in place — these are the ones the "Migrate to V2" button targets.
    // Goes straight at the raw SlackTeam storage rather than the SlackInstallation domain
    // object: the domain only ever surfaces a channel when FplBotSlackChannel is set, which
    // isn't the same question as "does this team have V1 subscriptions".
    internal static async Task<IResult> GetLegacyTeams(string? query, int page, int pageSize, ISlackTeamRepository teamRepo)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 25 : Math.Min(pageSize, 100);

        var teams = (await teamRepo.GetAllTeamsLegacyDoNotUse()).ToList();

        var legacy = new List<SlackTeam>();
        foreach (var team in teams)
        {
            if (!team.Subscriptions.Any())
            {
                continue;
            }

            var v2Channels = await teamRepo.GetChannelSubscriptions(team.TeamId!);
            if (!v2Channels.Any())
            {
                legacy.Add(team);
            }
        }

        var filtered = string.IsNullOrWhiteSpace(query)
            ? legacy
            : legacy.Where(t =>
                t.TeamName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (t.TeamId?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToLegacyDto)
            .ToList();

        return TypedResults.Ok(new PagedResult<TeamSummaryDto>(items, page, pageSize, filtered.Count));
    }

    private static TeamSummaryDto ToLegacyDto(SlackTeam team)
    {
        var channels = string.IsNullOrEmpty(team.FplBotSlackChannel)
            ? Enumerable.Empty<ChannelSubscriptionDto>()
            : [new ChannelSubscriptionDto(team.TeamId!, team.FplBotSlackChannel, team.FplbotLeagueId, team.Subscriptions)];
        return new(team.TeamId!, team.TeamName, channels, team.PendingRemoval ?? false);
    }

    internal static async Task<IResult> MigrateToV2(string teamId, ISlackTeamRepository teamRepo)
    {
        var teamIdToUpper = teamId.ToUpper();
        var legacyTeam = await teamRepo.FindTeamLegacyDoNotUse(teamIdToUpper);
        if (legacyTeam == null) return TypedResults.NotFound();

        await teamRepo.Save(SlackTeamRepository.ToDomainV1(legacyTeam));

        return TypedResults.Ok(new { migrated = true, message = $"Migrated {teamIdToUpper} to V2." });
    }

    // Renders from V2 (already loaded onto the installation) once a team has been migrated;
    // falls back to V1 channel subscriptions for teams that haven't been migrated yet.
    private static async Task<TeamSummaryDto> ToDto(SlackInstallation installation, ISlackTeamRepository teamRepo)
    {
        var channels = installation.ChannelSubscriptions.Select(c => ToDto(installation.TeamId, c)).ToList();
        if (channels.Count == 0)
        {
            var legacyTeam = await teamRepo.FindTeamLegacyDoNotUse(installation.TeamId);
            if (legacyTeam is not null)
            {
                channels = ToLegacyDto(legacyTeam).Subscriptions.ToList();
            }
        }

        return new(installation.TeamId, installation.TeamName, channels, installation.PendingRemoval);
    }

    private static ChannelSubscriptionDto ToDto(string teamId, SlackChannelSubscription channel) =>
        new(teamId, channel.ChannelId, channel.FollowedLeagueId is { } id ? (int)id.Value : null, ToEventSubscriptions(channel));

    private static IEnumerable<EventSubscription> ToEventSubscriptions(SlackChannelSubscription? channel) =>
        channel?.Events.Current.Select(e => Enum.Parse<EventSubscription>(e.ToString())) ?? [];

    private static IEnumerable<SlackChannelSubscription> ToLegacyChannels(SlackTeam team) =>
        string.IsNullOrEmpty(team.FplBotSlackChannel)
            ? []
            : [SlackChannelSubscription.Load(
                team.FplBotSlackChannel,
                team.FplbotLeagueId is { } id ? new ClassicLeagueId(id) : null,
                team.Subscriptions.Select(s => Enum.Parse<FplEvent>(s.ToString())))];

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

        var legacyTeam = await teamRepo.FindTeamLegacyDoNotUse(installation.TeamId);

        var isV2 = installation.ChannelSubscriptions.Count > 0;
        var channelSubscriptions = isV2
            ? installation.ChannelSubscriptions
            : legacyTeam is null ? [] : ToLegacyChannels(legacyTeam);
        var source = isV2 ? "v2" : "v1";

        var channels = new List<object>();
        foreach (var channel in channelSubscriptions)
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
                channelStatus,
                source
            });
        }

        object? legacy = null;
        if (legacyTeam is not null &&
            (!string.IsNullOrEmpty(legacyTeam.FplBotSlackChannel) || legacyTeam.FplbotLeagueId.HasValue || legacyTeam.Subscriptions.Any()))
        {
            legacy = new
            {
                scope = legacyTeam.Scope,
                accessToken = legacyTeam.AccessToken,
                channel = legacyTeam.FplBotSlackChannel,
                leagueId = legacyTeam.FplbotLeagueId,
                subscriptions = legacyTeam.Subscriptions,
                pendingRemoval = legacyTeam.PendingRemoval
            };
        }

        return TypedResults.Ok(new
        {
            teamId = installation.TeamId,
            teamName = installation.TeamName,
            token = installation.Token,
            pendingRemoval = installation.PendingRemoval,
            channels,
            legacy
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
        if (channels.Count == 0)
        {
            var legacyTeam = await teamRepo.FindTeamLegacyDoNotUse(installation.TeamId);
            if (legacyTeam is not null)
            {
                channels = ToLegacyChannels(legacyTeam).ToList();
            }
        }

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
}
