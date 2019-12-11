using System.Threading.Tasks;
using Fpl.Client;
using FplBot.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplClientTests
    {
        private IFplClient _client;

        public FplClientTests(ITestOutputHelper logger)
        {
            _client = Factory.CreateClient(logger);
        }
        
        [Fact]
        public async Task GetScoreBoard()
        {
            var scoreboard = await _client.GetScoreBoard("579157");
            Assert.NotEmpty(scoreboard.Standings.Results);
        }
        
        [Fact]
        public async Task GetBootstrap()
        {
            var bootstrap = await _client.GetBootstrap();
            Assert.NotEmpty(bootstrap.Elements);
            Assert.NotEmpty(bootstrap.Events);
        }
        
        [Fact]
        public async Task CacheTest()
        {
            await _client.GetBootstrap();
            await _client.GetBootstrap();
        }
    }
}