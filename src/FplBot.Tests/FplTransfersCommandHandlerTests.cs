using System.Threading.Tasks;
using FplBot.Tests.Helpers;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Extensions.FplBot.Handlers;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplTransfersCommandHandlerTests
    {
        private readonly IHandleMessages _client;

        public FplTransfersCommandHandlerTests(ITestOutputHelper logger)
        {
            _client = Factory.GetHandler<FplTransfersCommandHandler>(logger);
        }
        
        [Theory]
        [InlineData("@fplbot transfers")]
        [InlineData("<@UREFQD887> transfers")]
        [InlineData("<@UREFQD887> transfers 20")]
        public async Task GetPlayerHandler(string input)
        {
            var playerData = await _client.Handle(new SlackMessage
            {
                Text = input,
                ChatHub = new ChatHub(),
                Bot = Factory.MockBot
            });
            
            Assert.StartsWith("Transfers", playerData.HandledMessage);
        }
    }
}