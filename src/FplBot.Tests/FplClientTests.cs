using System;
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
            Assert.NotEmpty(standings);
        }
    }
}