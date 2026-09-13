using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Formatting.FixtureStats;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;

namespace FplBot.EventHandlers.Slack;

public class SlackFixtureEventsHandler(
    ISlackWorkSpacePublisher publisher,
    ISlackTeamRepository slackTeamRepo,
    ISlackClientBuilder service,
    ILeagueEntriesByGameweek leagueEntriesByGameweek,
    ITransfersByGameWeek transfersByGameWeek,
    IGlobalSettingsClient globalSettingsClient,
    ILogger<SlackFixtureEventsHandler> logger)
    : IConsumer<FixtureEventsOccured>, IConsumer<PublishFixtureEventsToSlackWorkspace>
{
    public async Task Consume(ConsumeContext<FixtureEventsOccured> context)
    {
        var message = context.Message;
        logger.LogInformation($"Handling {message.FixtureEvents.Count} new fixture events");
        var installations = await slackTeamRepo.GetAllInstallations();

        foreach (var installation in installations)
        {
            await context.Publish(new PublishFixtureEventsToSlackWorkspace(installation.TeamId, message.FixtureEvents), ctx => ctx.TimeToLive = TimeSpan.FromMinutes(30));
        }
    }

    public async Task Consume(ConsumeContext<PublishFixtureEventsToSlackWorkspace> context)
    {
        var message = context.Message;
        logger.LogInformation($"Publishing {message.FixtureEvents.Count} fixture events to {message.WorkspaceId}");
        var installation = await slackTeamRepo.GetInstallation(message.WorkspaceId);

        foreach (var sub in installation.ChannelSubscriptions)
        {
            await DoSubHandling(installation.TeamId, installation.Token, sub, message.FixtureEvents);
        }
    }

    private async Task DoSubHandling(string teamId, string? token, SlackChannelSubscription sub, List<FixtureEvents> fixtureEvents)
    {
        TauntData? tauntData = null;
        if (sub.IsSubscribedTo(FplEvent.Taunts) && sub.FollowedLeagueId is {} leagueId)
        {
            var gws = await globalSettingsClient.GetGlobalSettings();
            var currentGw = gws?.Gameweeks.GetCurrentGameweek();
            var slackUsers = await GetSlackUsers(token);
            IEnumerable<GameweekEntry> entries = new List<GameweekEntry>();
            IEnumerable<TransfersByGameWeek.Transfer> transfers = new List<TransfersByGameWeek.Transfer>();
            if (currentGw != null)
            {
                entries = await leagueEntriesByGameweek.GetEntriesForGameweek(currentGw.Id, (int)leagueId.Value);
                transfers = await transfersByGameWeek.GetTransfersByGameweek(currentGw.Id, (int)leagueId.Value);
            }

            tauntData = new TauntData(transfers, entries, entryName => SlackHandleHelper.GetSlackHandleOrFallback(slackUsers, entryName));
        }

        var eventMessages = GameweekEventsFormatter.FormatNewFixtureEvents(fixtureEvents, statType => ChannelHasStat(sub, statType), FormattingType.Slack, tauntData);
        var formattedStr = eventMessages.Select(evtMsg => $"{evtMsg.Title}\n{evtMsg.Details}");
        await publisher.PublishToWorkspace(teamId, sub.ChannelId, formattedStr.ToArray());
    }

    private static bool ChannelHasStat(SlackChannelSubscription channel, StatType statType)
    {
        var fplEvent = GetFplEventForStat(statType);
        return fplEvent.HasValue && channel.IsSubscribedTo(fplEvent.Value);
    }

    private static FplEvent? GetFplEventForStat(StatType statType) => statType switch
    {
        StatType.GoalsScored => FplEvent.FixtureGoals,
        StatType.Assists => FplEvent.FixtureAssists,
        StatType.OwnGoals => FplEvent.FixtureGoals,
        StatType.RedCards => FplEvent.FixtureCards,
        StatType.PenaltiesSaved => FplEvent.FixturePenaltyMisses,
        StatType.PenaltiesMissed => FplEvent.FixturePenaltyMisses,
        _ => null
    };

    private async Task<IEnumerable<User>> GetSlackUsers(string? accessToken)
    {
        var slackClient = service.Build(accessToken);

        try
        {
            var usersResponse = await slackClient.UsersList();
            if (usersResponse.Ok)
                return usersResponse.Members;
            return [];

        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return [];
        }
    }
}
