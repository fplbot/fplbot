using System.Threading.Tasks;
using FplBot.ConsoleApps.Clients;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplClientTests
    {
        private readonly ITestOutputHelper _logger;

        public FplClientTests(ITestOutputHelper logger)
        {
            _logger = logger;
        }
        
        [Fact]
        public async Task GetStandings()
        {
            var client = new FplClient();
            var standings = await client.GetStandings("579157");
            _logger.WriteLine(standings);
            Assert.NotEmpty(standings);
        }
        
        [Theory]
        [InlineData("salah")]
        [InlineData("mané")]
        [InlineData("firmino")]
        [InlineData("henderson")]
        [InlineData("wijnaldum")]
        [InlineData("tavares")]
        [InlineData("robertson")]
        [InlineData("van dijk")]
        [InlineData("matip")]
        [InlineData("trent")]
        [InlineData("alisson")]
        public async Task GetPlayer(string input)
        {
            var client = new FplClient();
            var playerData = await client.GetAllFplDataForPlayer(input);
            _logger.WriteLine(playerData);
            Assert.NotEmpty(playerData);
        }
    }
}