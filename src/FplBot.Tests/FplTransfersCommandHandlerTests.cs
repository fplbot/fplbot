using System;
using FplBot.Tests.Helpers;
using System.Threading.Tasks;
using FplBot.Core.Handlers;
using Slackbot.Net.Endpoints.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplTransfersCommandHandlerTests
    {
        private readonly IHandleAppMentions _client;

        public FplTransfersCommandHandlerTests(ITestOutputHelper logger)
        {
            _client = Factory.GetHandler<FplTransfersCommandHandler>(logger);
        }
        
        [Theory]
        [InlineData("@fplbot transfers")]
        [InlineData("<@UREFQD887> transfers")]
        public async Task GetTransfersHandlerShouldPostTransfers(string input)
        {
            var dummy = Factory.CreateDummyEvent(input);
            var transfers = await _client.Handle(dummy.meta, dummy.@event);
            Assert.Contains("Transfers", transfers.Response, StringComparison.InvariantCultureIgnoreCase);
        }
        
        [Theory]
        [InlineData("<@UREFQD887> transfers 20")]
        public async Task GetTransfersForExplicitGwShouldPostTransfersForGameweek(string input)
        {
            var dummy = Factory.CreateDummyEvent(input);
            var transfers = await _client.Handle(dummy.meta, dummy.@event);
            Assert.Contains("Transfers", transfers.Response, StringComparison.InvariantCultureIgnoreCase);
        }

        [Theory]
        [InlineData("@fplbot transfers 1")]
        [InlineData("<@UREFQD887> transfers 1")]
        public async Task GetTransfersHandlerForGw1ShouldPostSpecialMessage(string input)
        {
            var dummy = Factory.CreateDummyEvent(input);
            var transfers = await _client.Handle(dummy.meta, dummy.@event);

            Assert.Contains("Transfers", transfers.Response, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}