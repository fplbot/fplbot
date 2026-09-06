using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data.Slack;
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

public class FixtureEventsHandler : IConsumer<FixtureEventsOccured>, IConsumer<PublishFixtureEventsToSlackWorkspace>
{
    private readonly ISlackWorkSpacePublisher _publisher;
    private readonly ISlackTeamRepository _slackTeamRepo;
    private readonly ISlackClientBuilder _service;
    private readonly ILeagueEntriesByGameweek _leagueEntriesByGameweek;
    private readonly ITransfersByGameWeek _transfersByGameWeek;
    private readonly IGlobalSettingsClient _globalSettingsClient;
    private readonly ILogger<FixtureEventsHandler> _logger;

    public FixtureEventsHandler(ISlackWorkSpacePublisher publisher, ISlackTeamRepository slackTeamRepo, ISlackClientBuilder service, ILeagueEntriesByGameweek leagueEntriesByGameweek, ITransfersByGameWeek transfersByGameWeek, IGlobalSettingsClient globalSettingsClient, ILogger<FixtureEventsHandler> logger)
    {
        _publisher = publisher;
        _slackTeamRepo = slackTeamRepo;
        _service = service;
        _leagueEntriesByGameweek = leagueEntriesByGameweek;
        _transfersByGameWeek = transfersByGameWeek;
        _globalSettingsClient = globalSettingsClient;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<FixtureEventsOccured> context)
    {
        var message = context.Message;
        _logger.LogInformation($"Handling {message.FixtureEvents.Count} new fixture events");
        var slackTeams = await _slackTeamRepo.GetAllTeams();

        foreach (var slackTeam in slackTeams)
        {
            await context.Publish(new PublishFixtureEventsToSlackWorkspace(slackTeam.TeamId!, message.FixtureEvents), ctx => ctx.TimeToLive = TimeSpan.FromMinutes(30));
        }
    }

    public async Task Consume(ConsumeContext<PublishFixtureEventsToSlackWorkspace> context)
    {
        var message = context.Message;
        _logger.LogInformation($"Publishing {message.FixtureEvents.Count} fixture events to {message.WorkspaceId}");
        var slackTeam = await _slackTeamRepo.GetTeam(message.WorkspaceId);

        TauntData? tauntData = null;
        if (slackTeam.Subscriptions.ContainsSubscriptionFor(EventSubscription.Taunts) && slackTeam.FplbotLeagueId.HasValue)
        {
            var gws = await _globalSettingsClient.GetGlobalSettings();
            var currentGw = gws?.Gameweeks.GetCurrentGameweek();
            var slackUsers = await GetSlackUsers(slackTeam);
            IEnumerable<GameweekEntry> entries = new List<GameweekEntry>();
            IEnumerable<TransfersByGameWeek.Transfer> transfers = new List<TransfersByGameWeek.Transfer>();
            if (currentGw != null)
            {
                entries = await _leagueEntriesByGameweek.GetEntriesForGameweek(currentGw.Id, slackTeam.FplbotLeagueId.Value);
                transfers = await _transfersByGameWeek.GetTransfersByGameweek(currentGw.Id, slackTeam.FplbotLeagueId.Value);
            }

            tauntData = new TauntData(transfers, entries, entryName => SlackHandleHelper.GetSlackHandleOrFallback(slackUsers, entryName));
        }

        if(!string.IsNullOrEmpty(slackTeam.FplBotSlackChannel))
        {
            var eventMessages = GameweekEventsFormatter.FormatNewFixtureEvents(message.FixtureEvents, slackTeam.Subscriptions.ContainsStat, FormattingType.Slack, tauntData);
            var formattedStr = eventMessages.Select(evtMsg => $"{evtMsg.Title}\n{evtMsg.Details}");
            await _publisher.PublishToWorkspace(slackTeam.TeamId!, slackTeam.FplBotSlackChannel!, formattedStr.ToArray());
        }
    }

    private async Task<IEnumerable<User>> GetSlackUsers(SlackTeam t)
    {
        var slackClient = _service.Build(t.AccessToken);

        try
        {
            var usersResponse = await slackClient.UsersList();
            if (usersResponse.Ok)
                return usersResponse.Members;
            return Enumerable.Empty<User>();

        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return Enumerable.Empty<User>();
        }
    }
}
