using System.Threading.Tasks;
using FplBot.ConsoleApps;
using FplBot.Tests.Helpers;
using Slackbot.Net.Workers.Handlers;
using SlackConnector.Models;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplCommandHandlerTests
    {
        private readonly IHandleMessages _client;

        public FplCommandHandlerTests(ITestOutputHelper logger)
        {
            _client = Factory.GetHandler<FplCommandHandler>(logger);
        }
        
        [Theory]
        [InlineData("@fplbot fpl")]
        [InlineData("<@UREFQD887> fpl")]
        public async Task GetPlayerHandler(string input)
        {
            var playerData = await _client.Handle(new SlackMessage
            {
                Text = input,
                ChatHub = new SlackChatHub()
            });
            
            Assert.StartsWith(":star:", playerData.HandledMessage);
        }
    }
}