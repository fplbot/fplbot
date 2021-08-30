using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Core.Helpers;
using FplBot.Core.Models;
using FplBot.Messaging.Contracts.Events.v1;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace FplBot.Core.GameweekLifecycle
{
    internal class State
    {
        private readonly IFixtureClient _fixtureClient;
        private readonly IGlobalSettingsClient _settingsClient;
        private readonly IMediator _mediator;
        private readonly IMessageSession _session;
        private readonly ILogger<State> _logger;

        private ICollection<Player> _players;
        private ICollection<Fixture> _currentGameweekFixtures;
        private ICollection<Team> _teams;
        private int? _currentGameweek;

        public State(IFixtureClient fixtureClient,IGlobalSettingsClient settingsClient, IMediator mediator, IMessageSession session, ILogger<State> logger = null)
        {
            _fixtureClient = fixtureClient;
            _settingsClient = settingsClient;
            _mediator = mediator;
            _session = session;
            _logger = logger;

            _currentGameweekFixtures = new List<Fixture>();
            _players = new List<Player>();
            _teams = new List<Team>();
        }

        public async Task Reset(int newGameweek)
        {
            _currentGameweek = newGameweek;
            _currentGameweekFixtures = await _fixtureClient.GetFixturesByGameweek(newGameweek);
            var settings = await _settingsClient.GetGlobalSettings();
            _players = settings.Players;
            _teams = settings.Teams;
        }

        public async Task Refresh(int currentGameweek)
        {
            var latest = await _fixtureClient.GetFixturesByGameweek(currentGameweek);
            var fixtureEvents = LiveEventsExtractor.GetUpdatedFixtureEvents(latest, _currentGameweekFixtures);
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
                var fixtureUpdates = new FixtureUpdates
                {
                    CurrentGameweek = _currentGameweek.Value,
                    Teams = _teams,
                    Players = _players,
                    Events = fixtureEvents
                };
                await _mediator.Publish(new FixtureEventsOccured(fixtureUpdates));
            }

            if (priceChanges.Any())
                await _session.Publish(new PlayersPriceChanged { PlayersWithPriceChanges = priceChanges.ToList() });

            if (injuryUpdates.Any())
                await _mediator.Publish(new InjuryUpdateOccured(injuryUpdates));

            if (finishedFixtures.Any())
                await _mediator.Publish(new FixturesFinished(finishedFixtures.ToList()));

            if (newPlayers.Any())
                await _session.Publish(new NewPlayersRegistered(newPlayers));
        }
    }
}
