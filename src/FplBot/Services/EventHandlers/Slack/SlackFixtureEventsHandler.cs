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
        var channel = installation.PrimaryChannel();

        TauntData? tauntData = null;
        if (channel is not null && channel.IsSubscribedTo(FplEvent.Taunts) && channel.FollowedLeagueId is not null)
        {
            var leagueId = (int)channel.FollowedLeagueId.Value;
            var gws = await globalSettingsClient.GetGlobalSettings();
            var currentGw = gws?.Gameweeks.GetCurrentGameweek();
            var slackUsers = await GetSlackUsers(installation.Token);
            IEnumerable<GameweekEntry> entries = new List<GameweekEntry>();
            IEnumerable<TransfersByGameWeek.Transfer> transfers = new List<TransfersByGameWeek.Transfer>();
            if (currentGw != null)
            {
                entries = await leagueEntriesByGameweek.GetEntriesForGameweek(currentGw.Id, leagueId);
                transfers = await transfersByGameWeek.GetTransfersByGameweek(currentGw.Id, leagueId);
            }

            tauntData = new TauntData(transfers, entries, entryName => SlackHandleHelper.GetSlackHandleOrFallback(slackUsers, entryName));
        }

        if(channel is not null)
        {
            var eventMessages = GameweekEventsFormatter.FormatNewFixtureEvents(message.FixtureEvents, installation.ContainsStat, FormattingType.Slack, tauntData);
            var formattedStr = eventMessages.Select(evtMsg => $"{evtMsg.Title}\n{evtMsg.Details}");
            await publisher.PublishToWorkspace(installation.TeamId, channel.ChannelId, formattedStr.ToArray());
        }
    }

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
