using System.Threading.Tasks;
using FplBot.Tests.Helpers;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Extensions.FplBot.Handlers;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplInjuryCommandHandlerTests
    {
        private readonly IHandleEvent _client;

        public FplInjuryCommandHandlerTests(ITestOutputHelper logger)
        {
            _client = Factory.GetHandler<FplInjuryCommandHandler>(logger);
        }

        [Theory]
        [InlineData("@fplbot injuries")]
        public async Task GetPlayerHandler(string input)
        {
            var dummyEvent = Factory.CreateDummyEvent(input);
            var playerData = await _client.Handle(dummyEvent.meta, dummyEvent.@event);
            Assert.NotEmpty(playerData.Response);
        }
    }
}