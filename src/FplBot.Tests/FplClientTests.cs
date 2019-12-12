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
        private readonly IGlobalSettingsClient _globalSettingsClient;

        public FplClientTests(ITestOutputHelper logger)
        {
            _leagueClient = Factory.Create<ILeagueClient>(logger);
            _globalSettingsClient = Factory.Create<IGlobalSettingsClient>(logger);
        }
        
        [Fact]
        public async Task GetClassicLeague()
        {
            var scoreboard = await _leagueClient.GetClassicLeague(579157);
            Assert.NotEmpty(scoreboard.Standings.Entries);
        }
        
        [Fact]
        public async Task GetGlobalSettings()
        {
            var bootstrap = await _globalSettingsClient.GetGlobalSettings();
            Assert.NotEmpty(bootstrap.Players);
            Assert.NotEmpty(bootstrap.Events);
        }
        
        [Fact]
        public async Task CacheTest()
        {
            await _globalSettingsClient.GetGlobalSettings();
            await _globalSettingsClient.GetGlobalSettings();
        }
    }
}