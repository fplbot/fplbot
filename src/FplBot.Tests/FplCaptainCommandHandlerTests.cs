using FplBot.Tests.Helpers;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Extensions.FplBot.Handlers;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplCaptainCommandHandlerTests
    {
        private readonly IHandleMessages _client;

        public FplCaptainCommandHandlerTests(ITestOutputHelper logger)
        {
            _client = Factory.GetHandler<FplCaptainCommandHandler>(logger);
        }
        
        [Theory]
        [InlineData("@fplbot captains")]
        [InlineData("<@UREFQD887> captains")]
        public async Task GetCaptainsShouldPostAllEntryCaptainPicks(string input)
        {
            var playerData = await _client.Handle(Factory.CreateDummy(input));
            
            Assert.StartsWith(":boom:", playerData.HandledMessage);
        }

        [Theory]
        [InlineData("@fplbot captains 1")]
        [InlineData("<@UREFQD887> captains 1")]
        public async Task GetCaptainsForGameweekShouldPostAllEntryCaptainPicksForThatGameweek(string input)
        {
            var playerData = await _client.Handle(Factory.CreateDummy(input));

            Assert.StartsWith(":boom:", playerData.HandledMessage);
        }

        [Theory]
        [InlineData("@fplbot captains chart")]
        [InlineData("<@UREFQD887> captains chart")]
        [InlineData("<@UREFQD887> captains chart 19")]
        [InlineData("<@UREFQD887> captains 19 chart")]
        public async Task GetCaptainsChartShouldPostAllEntryCaptainPicksInAChart(string input)
        {
            var playerData = await _client.Handle(Factory.CreateDummy(input));

            Assert.StartsWith(":bar_chart:", playerData.HandledMessage);
        }
    }
}