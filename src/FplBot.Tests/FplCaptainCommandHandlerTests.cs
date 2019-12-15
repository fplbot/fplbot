using System.Threading.Tasks;
using FplBot.ConsoleApps.Handlers;
using FplBot.Tests.Helpers;
using Slackbot.Net.Workers.Handlers;
using SlackConnector.Models;
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
        public async Task GetCurrentGameweekHandler(string input)
        {
            var playerData = await _client.Handle(new SlackMessage
            {
                Text = input,
                ChatHub = new SlackChatHub()
            });
            
            Assert.StartsWith(":boom:", playerData.HandledMessage);
        }

        [Theory]
        [InlineData("@fplbot captains 1")]
        [InlineData("<@UREFQD887> captains 1")]
        public async Task GetGameweekOneHandler(string input)
        {
            var playerData = await _client.Handle(new SlackMessage
            {
                Text = input,
                ChatHub = new SlackChatHub()
            });

            Assert.StartsWith(":boom:", playerData.HandledMessage);
        }
    }
}