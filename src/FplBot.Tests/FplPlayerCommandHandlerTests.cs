using System.Threading.Tasks;
using FplBot.ConsoleApps;
using FplBot.Tests.Helpers;
using Slackbot.Net.Workers.Handlers;
using SlackConnector.Models;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplPlayerCommandHandlerTests
    {
        private readonly IHandleMessages _client;

        public FplPlayerCommandHandlerTests(ITestOutputHelper logger)
        {
            _client = Factory.GetHandler<FplPlayerCommandHandler>(logger);
        }
        
        [Theory]
        [InlineData("@fplbot player salah")]
        [InlineData("<@UREFQD887> player salah")]
        public async Task GetPlayerHandler(string input)
        {
            var playerData = await _client.Handle(new SlackMessage
            {
                Text = input,
                ChatHub = new SlackChatHub()
            });
            
            Assert.Equal("Mohamed Salah", playerData.HandledMessage);
        }

        [Theory]
        [InlineData("@fplbot player nonexistantplayer")]
        [InlineData("<@UREFQD887> player nonexistantplayer")]
        public async Task GetPlayerHandlerNonPlayer(string input)
        {
            var playerData = await _client.Handle(new SlackMessage
            {
                Text = input,
                ChatHub = new SlackChatHub()
            });
            
            Assert.Equal("Not found", playerData.HandledMessage);
        }

       
    }
}