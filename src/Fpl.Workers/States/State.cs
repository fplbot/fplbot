using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Core.Helpers;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace FplBot.Core.GameweekLifecycle
{
    internal class State
    {
        private readonly IFixtureClient _fixtureClient;
        private readonly IGlobalSettingsClient _settingsClient;
        private readonly IMessageSession _session;
        private readonly ILogger<State> _logger;

        private ICollection<Player> _players;
        private ICollection<Fixture> _currentGameweekFixtures;
        private ICollection<Team> _teams;

        public State(IFixtureClient fixtureClient,IGlobalSettingsClient settingsClient, IMessageSession session, ILogger<State> logger = null)
        {
            _fixtureClient = fixtureClient;
            _settingsClient = settingsClient;
            _session = session;
            _logger = logger;

            _currentGameweekFixtures = new List<Fixture>();
            _players = new List<Player>();
            _teams = new List<Team>();
        }

        public async Task Reset(int newGameweek)
        {
            _currentGameweekFixtures = await _fixtureClient.GetFixturesByGameweek(newGameweek);
            var settings = await _settingsClient.GetGlobalSettings();
            _players = settings.Players;
            _teams = settings.Teams;
        }

        public async Task Refresh(int currentGameweek)
        {
            var latest = await _fixtureClient.GetFixturesByGameweek(currentGameweek);
            var fixtureEvents = LiveEventsExtractor.GetUpdatedFixtureEvents(latest, _currentGameweekFixtures, _players, _teams);
            var finishedFixtures = LiveEventsExtractor.GetProvisionalFinishedFixtures(latest, _currentGameweekFixtures, _teams, _players);
            _currentGameweekFixtures = latest;

            var globalSettings = await _settingsClient.GetGlobalSettings();
            var after = globalSettings.Players;
            var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after, _players, _teams);
            var injuryUpdates = PlayerChangesEventsExtractor.GetInjuryUpdates(after, _players, _teams);
            var newPlayers = PlayerChangesEventsExtractor.GetNewPlayers(after, _players, _teams);

            _players = after;

            if (fixtureEvents.Any())
            {
                await _session.Publish(new FixtureEventsOccured(fixtureEvents.ToList()));
            }

            if (priceChanges.Any())
                await _session.Publish(new PlayersPriceChanged(priceChanges.ToList()));

            if (injuryUpdates.Any())
                await _session.Publish(new InjuryUpdateOccured(injuryUpdates));

            foreach (var fixture in finishedFixtures)
            {
                await _session.Publish(new FixtureFinished(fixture.Fixture.Id));
            }

            if (newPlayers.Any())
                await _session.Publish(new NewPlayersRegistered(newPlayers.ToList()));
        }
    }
}
