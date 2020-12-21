using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    public class State : IState
    {
        private readonly IFixtureClient _fixtureClient;
        private readonly IGlobalSettingsClient _settingsClient;

        private ICollection<Player> _players;
        private ICollection<Fixture> _currentGameweekFixtures;
        private ICollection<Team> _teams;
        private int? _currentGameweek;

        public State(IFixtureClient fixtureClient,IGlobalSettingsClient settingsClient)
        {
            _fixtureClient = fixtureClient;
            _settingsClient = settingsClient;

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
            var statusUpdates = PlayerChangesEventsExtractor.GetStatusChanges(after, _players, _teams);
            var transfersUpdates = PlayerChangesEventsExtractor.GetEventTransfers(after, _players, _teams);
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
                await OnNewFixtureEvents(fixtureUpdates);
            }

            if (priceChanges.Any())
            {
                await OnPriceChanges(priceChanges.ToList());
            }

            if (statusUpdates.Any())
            {
                await OnInjuryUpdates(statusUpdates.ToList());
            }
            
            if (finishedFixtures.Any())
                await OnFixturesProvisionalFinished(finishedFixtures.ToList());

            if (transfersUpdates.Any())
            {
                await OnTransfersUpdated(globalSettings.TotalPlayers, transfersUpdates);
            }
        }

        public event Func<FixtureUpdates, Task> OnNewFixtureEvents = (fixtureEvents) => Task.CompletedTask;
        public event Func<IEnumerable<PlayerUpdate>, Task> OnPriceChanges = (fixtureEvents) => Task.CompletedTask;
        public event Func<IEnumerable<PlayerUpdate>, Task> OnInjuryUpdates = statusUpdates => Task.CompletedTask;
        public event Func<long, IEnumerable<PlayerUpdate>, Task> OnTransfersUpdated = (totPlayers, transferUpdates) => Task.CompletedTask;
        public event Func<IEnumerable<FinishedFixture>, Task> OnFixturesProvisionalFinished = fixtures => Task.CompletedTask;
    }
}