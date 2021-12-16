using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Abstractions;
using Fpl.EventPublishers.Models.Mappers;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace Fpl.EventPublishers.States;

internal class LineupState
{
    private readonly IFixtureClient _fixtureClient;
    private readonly IGetMatchDetails _scraperApi;
    private readonly IMessageSession _session;
    private readonly ILogger<LineupState> _logger;
    private readonly Dictionary<int, MatchDetails> _matchDetails;

    public LineupState(IFixtureClient fixtureClient, IGetMatchDetails scraperApi, IMessageSession session, ILogger<LineupState> logger)
    {
        _fixtureClient = fixtureClient;
        _scraperApi = scraperApi;
        _session = session;
        _logger = logger;
        _matchDetails = new Dictionary<int, MatchDetails>();
    }

    public async Task Reset(int gw)
    {
        _matchDetails.Clear();
        ICollection<Fixture> fixtures;
        try
        {
            fixtures = await _fixtureClient.GetFixturesByGameweek(gw);
        }
        catch (HttpRequestException hre) when (LogError(hre))
        {
            return;
        }

        foreach (var fixture in fixtures)
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
        var fixtures = await _fixtureClient.GetFixturesByGameweek(gw);

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
                    _logger.LogWarning("Could not do match diff matchdetails for {PulseId}", new {fixture.PulseId});
                    _logger.LogInformation($"Contains({fixture.PulseId}): {_matchDetails.ContainsKey(fixture.PulseId)}");
                    _logger.LogInformation($"Details for ({fixture.PulseId})? : {updatedMatchDetails != null}");
                }

                if (updatedMatchDetails != null)
                {
                    _matchDetails[fixture.PulseId] = updatedMatchDetails;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }
    }

    private bool LogError(HttpRequestException hre)
    {
        _logger.LogWarning("Game is updating ({StatusCode})", hre.StatusCode);
        return hre.StatusCode == HttpStatusCode.ServiceUnavailable;
    }
}
