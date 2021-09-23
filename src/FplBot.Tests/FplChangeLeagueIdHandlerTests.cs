using System;
using FplBot.Tests.Helpers;
using System.Threading.Tasks;
using FplBot.Core.Handlers;
using Slackbot.Net.Endpoints.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplChangeLeagueIdHandlerTests
    {
        private readonly ITestOutputHelper _logger;
        private readonly IHandleAppMentions _client;

        public FplChangeLeagueIdHandlerTests(ITestOutputHelper logger)
        {
            _logger = logger;
            _client = Factory.GetHandler<FplChangeLeagueIdHandler>(logger);
        }

        [Fact]
        public async Task ChangeLeagueIdShouldUpdate()
        {
            var dummy = Factory.CreateDummyEvent("updateleagueid 126199");
            var response = await _client.Handle(dummy.meta, dummy.@event);
            _logger.WriteLine(response.Response);
            Assert.Contains("Thanks! You're now following", response.Response, StringComparison.InvariantCultureIgnoreCase);
        }

        [Fact]
        public async Task ChangeToInvalidLeagueIdShouldNotUpdate()
        {
            var dummy = Factory.CreateDummyEvent("updateleagueid abc");
            var response = await _client.Handle(dummy.meta, dummy.@event);
            _logger.WriteLine(response.Response);
            Assert.Contains("Could not update league to id 'abc'. Make sure it's a valid number.", response.Response, StringComparison.InvariantCultureIgnoreCase);
        }

        [Fact]
        public async Task ChangeToNotFoundLeagueIdShouldNotUpdate()
        {
            var dummy = Factory.CreateDummyEvent("updateleagueid 0");
            var response = await _client.Handle(dummy.meta, dummy.@event);
            _logger.WriteLine(response.Response);
            Assert.Contains("Could not find league 0 :/ Could you find it at https://fantasy.premierleague.com/leagues/0/standings/c ?", response.Response, StringComparison.InvariantCultureIgnoreCase);
        }

        [Fact]
        public async Task ChangeToMissingArgsProvidesHelpText()
        {
            var dummy = Factory.CreateDummyEvent("updateleagueid");
            var response = await _client.Handle(dummy.meta, dummy.@event);
            _logger.WriteLine(response.Response);
            Assert.Contains("No leagueId provided. Usage: `@fplbot updateleagueid 123`", response.Response, StringComparison.InvariantCultureIgnoreCase);
        }

        [Fact]
        public async Task ChangeToOtherNotFoundLeagueIdShouldNotUpdate()
        {
            var dummy = Factory.CreateDummyEvent("updateleagueid 11111111");
            var response = await _client.Handle(dummy.meta, dummy.@event);
            _logger.WriteLine(response.Response);
            Assert.Contains("Could not find league 11111111 :/ Could you find it at https://fantasy.premierleague.com/leagues/11111111/standings/c ?", response.Response, StringComparison.InvariantCultureIgnoreCase);
        }

    }
}
