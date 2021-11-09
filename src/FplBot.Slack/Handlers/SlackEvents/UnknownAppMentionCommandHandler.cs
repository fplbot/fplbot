using System.Text.RegularExpressions;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Slack.Handlers.SlackEvents;

public class UnknownAppMentionCommandHandler : INoOpAppMentions
{
    private readonly IMessageSession _session;
    private readonly IGlobalSettingsClient _settingsClient;
    private readonly IFixtureClient _fixtureClient;
    private readonly ILogger<UnknownAppMentionCommandHandler> _logger;

    public UnknownAppMentionCommandHandler(IMessageSession session, IGlobalSettingsClient settingsClient, IFixtureClient fixtureClient, ILogger<UnknownAppMentionCommandHandler> logger)
    {
        _session = session;
        _settingsClient = settingsClient;
        _fixtureClient = fixtureClient;
        _logger = logger;
    }
    public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent slackEvent)
    {
        string mentionTextStripped = Regex.Replace(slackEvent.Text, "<@(\\w+)>", "$1");
        await _session.Publish(new UnknownAppMentionReceived(eventMetadata.Team_Id, slackEvent.User, mentionTextStripped));
        return new EventHandledResponse("OK");
    }

    // To test fixture event:
    private async Task PublishPrevGwFixtureEvents(int gwId)
    {
        var settings = await _settingsClient.GetGlobalSettings();
        var updated = await _fixtureClient.GetFixturesByGameweek(gwId);
        var previous = await _fixtureClient.GetFixturesByGameweek(gwId);
        foreach (var fixture in previous)
        {
            fixture.Stats = Array.Empty<FixtureStat>();
            // if (fixture.Id == 24)
            // {
            //     //int before = fixture.Stats.First(s => s.Identifier == "goals_scored").HomeStats.First().Value;
            //     //int after = before - 2;
            //     //_logger.LogInformation($"Before: {before}. After {after}");
            //     //fixture.Stats.First(s => s.Identifier == "goals_scored").HomeStats.First().Value = after;
            //     var newl = fixture.Stats.First(s => s.Identifier == "goals_scored").HomeStats
            //         .Where(c => c.Value != 2).ToList().AsEnumerable();
            //     fixture.Stats.First(s => s.Identifier == "goals_scored").HomeStats = newl.ToList();
            // }
        }
        //
        // var allFixtureEvents = LiveEventsExtractor.GetUpdatedFixtureEvents(updated,previous, settings.Players, settings.Teams);
        // await _session.Publish(new FixtureEventsOccured(allFixtureEvents.ToArray()[0..1].ToList()));
    }
}