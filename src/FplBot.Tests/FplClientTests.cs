using System;
using System.Threading.Tasks;
using FplBot.ConsoleApps.Clients;
using Xunit;

namespace FplBot.Tests
{
    public class FplClientTests
    {
        [Fact]
        public async Task GetStandings()
        {
            var client = new FplClient();
            var standings = await client.GetStandings("579157");
            Assert.NotEmpty(standings);
        }
    }
}