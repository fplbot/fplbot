using FplBot.Tests.Helpers;
using Slackbot.Net.Extensions.FplBot.Handlers;
using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplSearchCommandHandlerTests
    {
        private readonly IHandleAppMentions _client;

        public FplSearchCommandHandlerTests(ITestOutputHelper logger)
        {
            _client = Factory.GetHandler<FplSearchHandler>(logger);
        }

        [Fact(Skip = "integration")]
        public async Task SearchForSkjelbek()
        {
            var dummy = Factory.CreateDummyEvent("search skjelbek");
            var playerData = await _client.Handle(dummy.meta, dummy.@event);
            Assert.Equal("Matching teams:\n" +
                         ":black_small_square: <https://fantasy.premierleague.com/entry/192197/event/9|Kun Magüero> (Magnus Skjelbek)\n" +
                         ":black_small_square: <https://fantasy.premierleague.com/entry/76744/event/9|van de skjelbeek> (Lars Skjelbek)\n" +
                         ":black_small_square: <https://fantasy.premierleague.com/entry/3558015/event/9|Anders Balleklubb> (Anders Skjelbek)\n\n" +
                         "Matching leagues:\nFound no matching leagues :shrug:", playerData.Response);
        }
    }
}