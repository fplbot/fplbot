using System.Net;
using System.Text;
using System.Text.Json;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.PulseLive;
using Fpl.EventPublishers.Extensions;
using Fpl.EventPublishers.Models.Mappers;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace Fpl.EventPublishers.States;

internal class LineupState(
    IFixtureClient fixtureClient,
    IPulseLiveClient pulseClient,
    IGlobalSettingsClient globalSettingsClient,
    IServiceScopeFactory scopeFactory,
    ILogger<LineupState> logger)
    : ILineupState
{
    private readonly Dictionary<int, MatchDetails> _matchDetails = new();
    private ICollection<Fixture> _currentFixtures = new List<Fixture>();
    private Dictionary<int, string?> _teamShortNames = new();

    public async Task Reset(int gw)
    {
        _matchDetails.Clear();
        try
        {
            _currentFixtures = await fixtureClient.GetFixturesByGameweek(gw) ?? new List<Fixture>();
            var settings = await globalSettingsClient.GetGlobalSettings();
            _teamShortNames = settings?.Teams.ToDictionary(t => t.Id, t => t.ShortName) ?? new Dictionary<int, string?>();
        }
        catch (Exception e) when (LogError(e))
        {
            return;
        }

        foreach (var fixture in _currentFixtures)
        {
            var lineups = await pulseClient.GetMatchDetails(fixture.Code);
            if (lineups != null)
            {
                _matchDetails[fixture.Code] = lineups;
            }
            else
            {
                // retry:
                var retry = await pulseClient.GetMatchDetails(fixture.Code);
                if (retry != null)
                {
                    _matchDetails[fixture.Code] = retry;
                }
            }
        }
    }

    public async Task Refresh(int gw)
    {
        ICollection<Fixture>? updatedFixtures;
        try
        {
            updatedFixtures = await fixtureClient.GetFixturesByGameweek(gw);
        }
        catch (Exception e) when (LogError(e))
        {
            return;
        }

        if (updatedFixtures == null)
            return;

        await CheckForLineups(updatedFixtures);
        await CheckForRemovedFixtures(updatedFixtures, gw);
        _currentFixtures = updatedFixtures;
    }

    private async Task CheckForRemovedFixtures(ICollection<Fixture> updatedFixtures, int gw)
    {
        using var scope = logger.AddContext("CheckForRemovedFixtures");
        var currentEvent = _currentFixtures.First().Event;
        var updatedEvent = updatedFixtures.First().Event;
        if (updatedEvent != currentEvent)
        {
            logger.LogWarning("Checking fixtures for different gameweek. {Current} vs {Updated}. Aborting.", currentEvent, updatedEvent );
            return;
        }

        foreach (var currentFixture in _currentFixtures)
        {
            try
            {
                var isFixtureRemoved = updatedFixtures.All(f => f.Id != currentFixture.Id);
                if (isFixtureRemoved)
                {
                    var settings = await globalSettingsClient.GetGlobalSettings();
                    var teams = settings?.Teams ?? new List<Team>();
                    var homeTeam = teams.First(t => t.Id == currentFixture.HomeTeamId);
                    var awayTeam = teams.First(t => t.Id == currentFixture.AwayTeamId);
                    var removedFixture = new RemovedFixture(currentFixture.Id,
                        new (homeTeam.Id, homeTeam.Name ?? string.Empty, homeTeam.ShortName ?? string.Empty),
                        new (awayTeam.Id, awayTeam.Name ?? string.Empty, awayTeam.ShortName ?? string.Empty));
                    await PublishAsync(new FixtureRemovedFromGameweek(gw, removedFixture));
                }
                else
                {
                    logger.LogTrace("Fixture {FixtureId} not removed", currentFixture.Id);
                }
            }
            catch (Exception e) when (LogError(e))
            {
            }
        }
    }

    private async Task CheckForLineups(ICollection<Fixture> fixtures)
    {
        using var scope = logger.AddContext("CheckForLineups");
        foreach (var fixture in fixtures.Where(f => f.Started != true))
        {
            try
            {
                var updatedMatchDetails = await pulseClient.GetMatchDetails(fixture.Code);
                if (_matchDetails.ContainsKey(fixture.Code) && updatedMatchDetails != null)
                {
                    var storedDetails = _matchDetails[fixture.Code];
                    var lineupsConfirmed = !storedDetails.HasLineUps() && updatedMatchDetails.HasLineUps();
                    if (lineupsConfirmed)
                    {
                        var homeAbbr = _teamShortNames.GetValueOrDefault(fixture.HomeTeamId, "?") ?? "?";
                    var awayAbbr = _teamShortNames.GetValueOrDefault(fixture.AwayTeamId, "?") ?? "?";
                    var lineups = MatchDetailsMapper.TryMapToLineup(updatedMatchDetails, fixture.Code, homeAbbr, awayAbbr, e => logger.LogError(e, e.Message));

                        if (lineups != null)
                        {
                            await PublishAsync(lineups);
                        }
                        else
                        {
                            logger.LogWarning("FAILED TO PUBLISH LINEUPS FOR {PulseId}", new { fixture.Code });
                            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
                            {
                                WriteIndented = true
                            };
                            logger.LogWarning(System.Text.Json.JsonSerializer.Serialize(updatedMatchDetails, options));
                        }
                    }
                }
                else
                {
                    logger.LogWarning("Could not do match diff matchdetails for {PulseId}", new { fixture.Code });
                    logger.LogDebug($"Contains({fixture.Code}): {_matchDetails.ContainsKey(fixture.Code)}");
                    logger.LogDebug($"Details for ({fixture.Code})? : {updatedMatchDetails != null}");
                }

                if (updatedMatchDetails != null)
                {
                    _matchDetails[fixture.Code] = updatedMatchDetails;
                }
            }
            catch (Exception e) when (LogError(e))
            {
            }
        }
    }

    private bool LogError(Exception e)
    {
        if (e is HttpRequestException { StatusCode: HttpStatusCode.ServiceUnavailable })
        {
            logger.LogWarning("Game is updating");
        }
        else
        {
            logger.LogError(e, e.Message);
        }

        return true;
    }

    private async Task PublishAsync<T>(T message) where T : class
    {
        using var scope = scopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        await publisher.Publish(message);
    }

    public void LogState()
    {
        StringBuilder logstring = new ($"Debug. \nCurrent state has ({_matchDetails.Keys.Count} fixtures):");
        foreach (var key in _matchDetails.Keys)
        {
            logstring.Append($"\n{key} - Lineups: {_matchDetails[key].HomeTeam?.TeamId}-{_matchDetails[key].AwayTeam?.TeamId} {_matchDetails[key].HasLineUps()}");
        }
        logger.LogInformation(logstring.ToString());
    }
}
