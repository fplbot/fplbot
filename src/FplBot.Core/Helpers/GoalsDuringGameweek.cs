using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client;
using Fpl.Client.Abstractions;
using Fpl.Workers;
using FplBot.Core.Abstractions;
using FplBot.Core.Extensions;

namespace FplBot.Core.Helpers
{
    internal class GoalsDuringGameweek : IGoalsDuringGameweek
    {
        private readonly IFixtureClient _fixtureClient;

        public GoalsDuringGameweek(IFixtureClient fixtureClient)
        {
            _fixtureClient = fixtureClient;
        }

        public async Task<IDictionary<int, int>> GetPlayerGoals(int gameweek)
        {
            var fixtures = await _fixtureClient.GetFixturesByGameweek(gameweek);

            return fixtures
                .Select(fixture => fixture.Stats
                    .OrEmptyIfNull()
                    .SingleOrDefault(stat => stat.Identifier == FplConstants.StatIdentifiers.GoalsScored))
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
