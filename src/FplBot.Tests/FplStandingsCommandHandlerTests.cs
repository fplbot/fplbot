using System.Threading.Tasks;
using FplBot.Slack.Handlers.SlackEvents;
using FplBot.Tests.Helpers;
using Slackbot.Net.Endpoints.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplStandingsCommandHandlerTests
    {
        private readonly IHandleAppMentions _client;

        public FplStandingsCommandHandlerTests(ITestOutputHelper logger)
        {
            _client = Factory.GetHandler<FplStandingsCommandHandler>(logger);
        }

        [Theory(Skip = "Humbug før sesongen er i gang?")]
        [InlineData("@fplbot standings")]
        [InlineData("<@UREFQD887> standings")]

        public async Task GetStandings(string input)
        {
            var dummy = Factory.CreateDummyEvent(input);
            var playerData = await _client.Handle(dummy.meta, dummy.@event);
            Assert.DoesNotContain("Oops", playerData.Response);
        }
    }
}
