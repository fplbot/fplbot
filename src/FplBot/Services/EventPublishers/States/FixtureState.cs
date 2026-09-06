using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Extensions;
using Fpl.EventPublishers.Models.Mappers;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace Fpl.EventPublishers.States;

internal class FixtureState(
    IFixtureClient fixtureClient,
    IGlobalSettingsClient settingsClient,
    IServiceScopeFactory scopeFactory,
    ILogger<FixtureState> logger)
    : IFixtureState
{
    private ICollection<Player> _players = new List<Player>();
    private ICollection<Fixture> _currentGameweekFixtures = new List<Fixture>();
    private ICollection<Team> _teams = new List<Team>();

    public async Task Reset(int newGameweek)
    {
        using var scope = logger.AddContext("StateInit");
        logger.LogInformation($"Running reset for gw {newGameweek}");
        _currentGameweekFixtures = await fixtureClient.GetFixturesByGameweek(newGameweek) ?? new List<Fixture>();
        var settings = await settingsClient.GetGlobalSettings();
        _players = settings?.Players ?? new List<Player>();
        _teams = settings?.Teams ?? new List<Team>();
    }

    public async Task Refresh(int currentGameweek)
    {
        using var scope = logger.AddContext("StateRefresh");
        logger.LogInformation($"Refreshing {currentGameweek}");
        var latest = await fixtureClient.GetFixturesByGameweek(currentGameweek) ?? new List<Fixture>();
        var fixtureEvents = LiveEventsExtractor.GetUpdatedFixtureEvents(latest, _currentGameweekFixtures, _players, _teams);
        var finishedFixtures = LiveEventsExtractor.GetProvisionalFinishedFixtures(latest, _currentGameweekFixtures, _teams, _players);
        _currentGameweekFixtures = latest;

        var globalSettings = await settingsClient.GetGlobalSettings();
        var after = globalSettings?.Players ?? new List<Player>();

        _players = after;

        if (fixtureEvents.Any() || finishedFixtures.Any())
        {
            using var publishScope = scopeFactory.CreateScope();
            var publisher = publishScope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            if (fixtureEvents.Any())
                await publisher.Publish(new FixtureEventsOccured(fixtureEvents.ToList()), ctx => ctx.TimeToLive = TimeSpan.FromMinutes(30));
            foreach (var fixture in finishedFixtures)
                await publisher.Publish(new FixtureFinished(fixture));
        }
    }
}
