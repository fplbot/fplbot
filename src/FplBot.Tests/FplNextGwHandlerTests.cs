using System.Threading.Tasks;
using FplBot.Tests.Helpers;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Extensions.FplBot.Handlers;
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
                ChatHub = new ChatHub()
            });
            
            Assert.NotEmpty(playerData.HandledMessage);
        }
    }
}