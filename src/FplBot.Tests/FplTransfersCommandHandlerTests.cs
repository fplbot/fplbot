using FplBot.Tests.Helpers;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Extensions.FplBot.Handlers;
using System.Threading.Tasks;
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
        public async Task GetTransfersHandlerShouldPostTransfers(string input)
        {
            var playerData = await _client.Handle(new SlackMessage
            {
                Text = input,
                ChatHub = new ChatHub(),
                Bot = Factory.MockBot
            });
            
            Assert.StartsWith("Transfers", playerData.HandledMessage);
        }

        [Theory]
        [InlineData("@fplbot transfers 1")]
        [InlineData("<@UREFQD887> transfers 1")]
        public async Task GetTransfersHandlerForGw1ShouldPostSpecialMessage(string input)
        {
            var playerData = await _client.Handle(new SlackMessage
            {
                Text = input,
                ChatHub = new ChatHub(),
                Bot = Factory.MockBot
            });

            Assert.Equal("No transfers are made the first gameweek.", playerData.HandledMessage);
        }
    }
}