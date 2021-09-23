using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Core.Extensions;
using FplBot.Core.Helpers;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Core.Handlers.SlackEvents
{
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
            await _session.Publish(new UnknownAppMentionReceived(eventMetadata.Team_Id, slackEvent.User, slackEvent.Text));
            return new EventHandledResponse("OK");
        }

        // To test fixture event:
        private async Task PublishPrevGwFixtureEvents(int gwId)
        {
            var settings = await _settingsClient.GetGlobalSettings();
            var fixturesWithStats = await _fixtureClient.GetFixturesByGameweek(gwId);
            var fixturesWithoutStats = await _fixtureClient.GetFixturesByGameweek(gwId);
            foreach (var fixture in fixturesWithoutStats)
            {
                fixture.Stats = Array.Empty<FixtureStat>();
            }

            var allFixtureEvents = LiveEventsExtractor.GetUpdatedFixtureEvents(fixturesWithStats, fixturesWithoutStats, settings.Players, settings.Teams);
            await _session.Publish(new FixtureEventsOccured(allFixtureEvents.ToArray()[^2..^1].ToList()));
        }
    }
}
