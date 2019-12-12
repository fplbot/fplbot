using System.Threading.Tasks;
using FplBot.ConsoleApps.Handlers;
using FplBot.Tests.Helpers;
using Slackbot.Net.Workers.Handlers;
using SlackConnector.Models;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplNextGwHandlerTests
    {
        private readonly IHandleMessages _client;

        public FplNextGwHandlerTests(ITestOutputHelper logger)
        {
            _client = Factory.GetHandler<FplNextGameweekCommandHandler>(logger);
        }
        
        [Theory]
        [InlineData("@fplbot nextgw")]
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