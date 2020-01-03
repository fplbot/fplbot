using System.Threading.Tasks;
using FplBot.Tests.Helpers;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Extensions.FplBot.Handlers;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplInjuryCommandHandlerTests
    {
        private readonly IHandleMessages _client;

        public FplInjuryCommandHandlerTests(ITestOutputHelper logger)
        {
            _client = Factory.GetHandler<FplInjuryCommandHandler>(logger);
        }

        [Theory]
        [InlineData("@fplbot injuries")]
        public async Task GetPlayerHandler(string input)
        {
            var playerData = await _client.Handle(new SlackMessage
            {
                Text = input,
                ChatHub = new ChatHub()
            });

            Assert.NotEmpty(playerData.HandledMessage);
        }
    }
}