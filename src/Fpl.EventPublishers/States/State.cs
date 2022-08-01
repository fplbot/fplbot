using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Extensions;
using Fpl.EventPublishers.Models.Mappers;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace Fpl.EventPublishers.States;

internal class State
{
    private readonly IFixtureClient _fixtureClient;
    private readonly IGlobalSettingsClient _settingsClient;
    private readonly IMessageSession _session;
    private readonly ILogger<State> _logger;

    private ICollection<Player> _players;
    private ICollection<Fixture> _currentGameweekFixtures;
    private ICollection<Team> _teams;

    public State(IFixtureClient fixtureClient,IGlobalSettingsClient settingsClient, IMessageSession session, ILogger<State> logger)
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
        using var scope = _logger.AddContext("StateInit");
        _logger.LogInformation($"Running reset for gw {newGameweek}");
        _currentGameweekFixtures = await _fixtureClient.GetFixturesByGameweek(newGameweek);
        var settings = await _settingsClient.GetGlobalSettings();
        _players = settings.Players;
        _teams = settings.Teams;
    }

    public async Task Refresh(int currentGameweek)
    {
        using var scope = _logger.AddContext("StateRefresh");
        _logger.LogInformation($"Refreshing {currentGameweek}");
        var latest = await _fixtureClient.GetFixturesByGameweek(currentGameweek);
        var fixtureEvents = LiveEventsExtractor.GetUpdatedFixtureEvents(latest, _currentGameweekFixtures, _players, _teams);
        var finishedFixtures = LiveEventsExtractor.GetProvisionalFinishedFixtures(latest, _currentGameweekFixtures, _teams, _players);
        _currentGameweekFixtures = latest;

        var globalSettings = await _settingsClient.GetGlobalSettings();
        var after = globalSettings.Players;

        _players = after;

        if (fixtureEvents.Any())
        {
            await _session.Publish(new FixtureEventsOccured(fixtureEvents.ToList()));
        }

        foreach (var fixture in finishedFixtures)
        {
            await _session.Publish(new FixtureFinished(fixture));
        }
    }
}
