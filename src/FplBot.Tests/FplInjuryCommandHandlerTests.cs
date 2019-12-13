using System.Threading.Tasks;
using FplBot.ConsoleApps.Handlers;
using FplBot.Tests.Helpers;
using Slackbot.Net.Workers.Handlers;
using SlackConnector.Models;
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
                ChatHub = new SlackChatHub()
            });

            Assert.NotEmpty(playerData.HandledMessage);
        }
    }
}