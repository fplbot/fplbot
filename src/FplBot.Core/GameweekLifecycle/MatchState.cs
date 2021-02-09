using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using FplBot.Core.Abstractions;
using FplBot.Core.Models;
using Microsoft.Extensions.Logging;

namespace FplBot.Core.GameweekLifecycle
{
    public class MatchState
    {
        private readonly IFixtureClient _fixtureClient;
        private readonly IGetMatchDetails _scraperApi;
        private readonly ILogger<MatchState> _logger;
        private Dictionary<int, MatchDetails> _matchDetails;

        public MatchState(IFixtureClient fixtureClient, IGetMatchDetails scraperApi, ILogger<MatchState> logger)
        {
            _fixtureClient = fixtureClient;
            _scraperApi = scraperApi;
            _logger = logger;
            _matchDetails = new Dictionary<int, MatchDetails>();
        }

        public async Task Reset(int gw)
        {
            _matchDetails.Clear();
            var fixtures = await _fixtureClient.GetFixturesByGameweek(gw);
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
                var updatedMatchDetails = await _scraperApi.GetMatchDetails(fixture.PulseId);
                if (_matchDetails.ContainsKey(fixture.PulseId) && updatedMatchDetails != null)
                {
                    var storedDetails = _matchDetails[fixture.PulseId];
                    var updates = MatchDetailsDiffer.Diff(updatedMatchDetails, storedDetails);
                    if (updates.LineupsConfirmed)
                    {
                        var lineups = MatchDetailsMapper.ToLineup(updatedMatchDetails);
                        await OnLineUpReady(lineups);
                    }
                }
                else
                {
                    _logger.LogWarning("Could do match diff matchdetails for {PulseId}", new {fixture.PulseId});
                    _logger.LogInformation($"Contains({fixture.PulseId}): {_matchDetails.ContainsKey(fixture.PulseId)}");
                    _logger.LogInformation($"Details for ({fixture.PulseId})? : {updatedMatchDetails != null}");
                }

                if(updatedMatchDetails != null)
                    _matchDetails[fixture.PulseId] = updatedMatchDetails;
            }
        }

        public event Func<Lineups, Task> OnLineUpReady = (fixtureEvents) => Task.CompletedTask;
    }

    public class MatchDetailsDiffer
    {
        public static MatchDetailsDiff Diff(MatchDetails after, MatchDetails before)
        {
            return new MatchDetailsDiff
            {
                LineupsConfirmed = !before.HasLineUps() && after.HasLineUps()
            };
        }
    }

    public class MatchDetailsDiff
    {
        public bool LineupsConfirmed { get; set; }
    }
}