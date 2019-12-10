using System.Net.Http;
using System.Threading.Tasks;
using FplBot.ConsoleApps;
using FplBot.ConsoleApps.Clients;
using FplBot.Tests.Helpers;
using SlackConnector.Models;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplCommandHandlerTests
    {
        private readonly ITestOutputHelper _logger;

        public FplCommandHandlerTests(ITestOutputHelper logger)
        {
            _logger = logger;
        }
        
        [Theory]
        [InlineData("@fplbot fpl")]
        [InlineData("<@UREFQD887> fpl")]
        public async Task GetPlayerHandler(string input)
        {
            var client = Factory.CreateFplHandler();
            var playerData = await client.Handle(new SlackMessage
            {
                Text = input,
                ChatHub = new SlackChatHub()
            });
            
            Assert.Equal("OK", playerData.HandledMessage);
        }
    }
}