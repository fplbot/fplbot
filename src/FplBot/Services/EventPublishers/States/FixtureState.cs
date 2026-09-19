using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Extensions;
using Fpl.EventPublishers.Models.Mappers;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace Fpl.EventPublishers.States;

public class FixtureState(
    IFixtureClient fixtureClient,
    IGlobalSettingsClient settingsClient,
    IServiceScopeFactory scopeFactory,
    ILogger<FixtureState> logger)
    : IFixtureState
{
    private ICollection<Player> _players = [];
    private ICollection<Fixture> _currentGameweekFixtures = [];
    private ICollection<Team> _teams = [];

    public async Task Reset(int newGameweek)
    {
        using var scope = logger.AddContext("StateInit");
        logger.LogInformation($"Running reset for gw {newGameweek}");
        _currentGameweekFixtures = await fixtureClient.GetFixturesByGameweek(newGameweek) ?? [];
        var settings = await settingsClient.GetGlobalSettings();
        _players = settings?.Players ?? [];
        _teams = settings?.Teams ?? [];
    }

    public async Task Refresh(int currentGameweek)
    {
        using var scope = logger.AddContext("StateRefresh");
        logger.LogInformation($"Refreshing {currentGameweek}");
        var latest = await fixtureClient.GetFixturesByGameweek(currentGameweek) ?? [];
        var fixtureEvents = LiveEventsExtractor.GetUpdatedFixtureEvents(latest, _currentGameweekFixtures, _players, _teams);
        var finishedFixtures = LiveEventsExtractor.GetProvisionalFinishedFixtures(latest, _currentGameweekFixtures, _teams, _players);
        _currentGameweekFixtures = latest;

        var globalSettings = await settingsClient.GetGlobalSettings();
        var after = globalSettings?.Players ?? [];

        _players = after;

        if (fixtureEvents.Any() || finishedFixtures.Any())
        {
            using var publishScope = scopeFactory.CreateScope();
            var publisher = publishScope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            if (fixtureEvents.Any())
                await publisher.Publish(new FixtureEventsOccured([.. fixtureEvents]), ctx => ctx.TimeToLive = TimeSpan.FromMinutes(30));
            foreach (var fixture in finishedFixtures)
                await publisher.Publish(new FixtureFinished(fixture));
        }
    }
}
