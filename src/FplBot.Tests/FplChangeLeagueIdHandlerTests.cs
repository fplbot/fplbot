using System;
using FplBot.Tests.Helpers;
using Slackbot.Net.Extensions.FplBot.Handlers;
using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplChangeLeagueIdHandlerTests
    {
        private readonly IHandleEvent _client;

        public FplChangeLeagueIdHandlerTests(ITestOutputHelper logger)
        {
            _client = Factory.GetHandler<FplChangeLeagueIdHandler>(logger);
        }
        
        [Fact]
        public async Task ChangeLeagueIdShouldUpdate()
        {
            var dummy = Factory.CreateDummyEvent("updateleagueid 1337");
            var response = await _client.Handle(dummy.meta, dummy.@event);
            Assert.Contains("Thanks! You're now following", response.Response, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}