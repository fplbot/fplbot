using System.Threading.Tasks;
using Fpl.Client;
using Fpl.Client.Abstractions;
using FplBot.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplClientTests
    {
        private readonly ILeagueClient _leagueClient;
        private readonly IGameweekClient _gameweekClient;

        public FplClientTests(ITestOutputHelper logger)
        {
            _leagueClient = Factory.Create<ILeagueClient>(logger);
            _gameweekClient = Factory.Create<IGameweekClient>(logger);
        }
        
        [Fact]
        public async Task GetClassicLeague()
        {
            var scoreboard = await _leagueClient.GetClassicLeague(15673);
            Assert.NotNull(scoreboard.Standings);
        }
        
        [Fact]
        public async Task GetGameweeks()
        {
            var gameweeks = await _gameweekClient.GetGameweeks();
            Assert.NotEmpty(gameweeks);
        }
        
        [Fact]
        public async Task CacheTest()
        {
            await _gameweekClient.GetGameweeks();
            await _gameweekClient.GetGameweeks();
        }
    }
}