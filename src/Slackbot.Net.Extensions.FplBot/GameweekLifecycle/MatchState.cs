using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Models;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle
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
                _matchDetails[fixture.PulseId] = lineups;
            }
        }

        public async Task Refresh(int gw)
        {
            var fixtures = await _fixtureClient.GetFixturesByGameweek(gw);

            foreach (var fixture in fixtures)
            {
                var updatedMatchDetails = await _scraperApi.GetMatchDetails(fixture.PulseId);
                var storedDetails = _matchDetails[fixture.PulseId];
                var updates = MatchDetailsDiffer.Diff(updatedMatchDetails, storedDetails);
                if (updates.LineupsConfirmed)
                {
                    var lineups = MatchDetailsMapper.ToLineup(updatedMatchDetails);
                    await OnLineUpReady(lineups);
                }

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