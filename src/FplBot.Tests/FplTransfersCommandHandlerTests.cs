using FplBot.Tests.Helpers;
using Slackbot.Net.Extensions.FplBot.Handlers;
using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplTransfersCommandHandlerTests
    {
        private readonly IHandleEvent _client;

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
            var dummy = Factory.CreateDummyEvent(input);
            var transfers = await _client.Handle(dummy.meta, dummy.@event);
            Assert.StartsWith("Transfers", transfers.Response);
        }

        [Theory]
        [InlineData("@fplbot transfers 1")]
        [InlineData("<@UREFQD887> transfers 1")]
        public async Task GetTransfersHandlerForGw1ShouldPostSpecialMessage(string input)
        {
            var dummy = Factory.CreateDummyEvent(input);
            var transfers = await _client.Handle(dummy.meta, dummy.@event);

            Assert.Equal("No transfers are made the first gameweek.", transfers.Response);
        }
    }
}