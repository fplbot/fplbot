using Fpl.Client.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal class GoalsDuringGameweek : IGoalsDuringGameweek
    {
        private readonly IFixtureClient _fixtureClient;

        public GoalsDuringGameweek(IFixtureClient fixtureClient)
        {
            _fixtureClient = fixtureClient;
        }

        public async Task<IDictionary<int, int>> GetGoalsByPlayerId(int gameweek)
        {
            var fixtures = await _fixtureClient.GetFixturesByGameweek(gameweek);

            return fixtures
                .Select(fixture => fixture.Stats
                    .OrEmptyIfNull()
                    .SingleOrDefault(stat => stat.Identifier == Constants.StatIdentifiers.GoalsScored))
                .WhereNotNull()
                .SelectMany(goalStatsPrFixture =>
                    goalStatsPrFixture?.HomeStats?.OrEmptyIfNull()
                        .Concat(goalStatsPrFixture?.AwayStats.OrEmptyIfNull())
                        .OrEmptyIfNull()
                        .Select(x => new {Player = x.Element, Goals = x.Value})
                )
                .ToDictionary(x => x.Player, x => x.Goals);
        }

    }
}
