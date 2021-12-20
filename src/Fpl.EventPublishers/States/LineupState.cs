using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Abstractions;
using Fpl.EventPublishers.Models.Mappers;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace Fpl.EventPublishers.States;

internal class LineupState
{
    private readonly IFixtureClient _fixtureClient;
    private readonly IGetMatchDetails _scraperApi;
    private readonly IGlobalSettingsClient _globalSettingsClient;
    private readonly IMessageSession _session;
    private readonly ILogger<LineupState> _logger;
    private readonly Dictionary<int, MatchDetails> _matchDetails;
    private ICollection<Fixture> _currentFixtures;

    public LineupState(IFixtureClient fixtureClient, IGetMatchDetails scraperApi, IGlobalSettingsClient globalSettingsClient, IMessageSession session, ILogger<LineupState> logger)
    {
        _fixtureClient = fixtureClient;
        _scraperApi = scraperApi;
        _globalSettingsClient = globalSettingsClient;
        _session = session;
        _logger = logger;
        _matchDetails = new Dictionary<int, MatchDetails>();
        _currentFixtures = new List<Fixture>();
    }

    public async Task Reset(int gw)
    {
        _matchDetails.Clear();
        try
        {
            _currentFixtures = await _fixtureClient.GetFixturesByGameweek(gw);
        }
        catch (Exception e) when (LogError(e))
        {
            return;
        }

        foreach (var fixture in _currentFixtures)
        {
            var lineups = await _scraperApi.GetMatchDetails(fixture.PulseId);
            if (lineups != null)
            {
                _matchDetails[fixture.PulseId] = lineups;
            }
            else
            {
                // retry:
                var retry = await _scraperApi.GetMatchDetails(fixture.PulseId);
                if (retry != null)
                {
                    _matchDetails[fixture.PulseId] = retry;
                }
            }
        }
    }

    public async Task Refresh(int gw)
    {
        ICollection<Fixture> updatedFixtures;
        try
        {
            updatedFixtures = await _fixtureClient.GetFixturesByGameweek(gw);
        }
        catch (Exception e) when (LogError(e))
        {
            return;
        }

        await CheckForLineups(updatedFixtures);
        await CheckForRemovedFixtures(updatedFixtures, gw);
        _currentFixtures = updatedFixtures;
    }

    private async Task CheckForRemovedFixtures(ICollection<Fixture> updatedFixtures, int gw)
    {
        foreach (var currentFixture in _currentFixtures)
        {
            try
            {
                var isFixtureRemoved = updatedFixtures.All(f => f.Id != currentFixture.Id);
                if (isFixtureRemoved)
                {
                    var settings = await _globalSettingsClient.GetGlobalSettings();
                    var teams = settings.Teams;
                    var homeTeam = teams.First(t => t.Id == currentFixture.HomeTeamId);
                    var awayTeam = teams.First(t => t.Id == currentFixture.AwayTeamId);
                    var removedFixture = new RemovedFixture(currentFixture.Id,
                        new (homeTeam.Id, homeTeam.Name, homeTeam.ShortName),
                        new (awayTeam.Id, awayTeam.Name, awayTeam.ShortName));
                    await _session.Publish(new FixtureRemovedFromGameweek(gw, removedFixture));
                }
            }
            catch (Exception e) when (LogError(e))
            {
            }
        }
    }

    private async Task CheckForLineups(ICollection<Fixture> fixtures)
    {
        foreach (var fixture in fixtures)
        {
            try
            {
                var updatedMatchDetails = await _scraperApi.GetMatchDetails(fixture.PulseId);
                if (_matchDetails.ContainsKey(fixture.PulseId) && updatedMatchDetails != null)
                {
                    var storedDetails = _matchDetails[fixture.PulseId];
                    var lineupsConfirmed = !storedDetails.HasLineUps() && updatedMatchDetails.HasLineUps();
                    if (lineupsConfirmed)
                    {
                        var lineups = MatchDetailsMapper.TryMapToLineup(updatedMatchDetails);

                        if (lineups != null)
                        {
                            await _session.Publish(lineups);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Could not do match diff matchdetails for {PulseId}", new { fixture.PulseId });
                    _logger.LogDebug($"Contains({fixture.PulseId}): {_matchDetails.ContainsKey(fixture.PulseId)}");
                    _logger.LogDebug($"Details for ({fixture.PulseId})? : {updatedMatchDetails != null}");
                }

                if (updatedMatchDetails != null)
                {
                    _matchDetails[fixture.PulseId] = updatedMatchDetails;
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
            _logger.LogWarning("Game is updating");
        }
        else
        {
            _logger.LogError(e, e.Message);
        }

        return true;
    }
}
