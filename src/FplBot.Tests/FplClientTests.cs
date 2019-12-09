using System.Collections.Generic;
using System.Threading.Tasks;
using FplBot.ConsoleApps;
using FplBot.ConsoleApps.Clients;
using Slackbot.Net.Workers.Publishers;
using SlackConnector.Models;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplClientTests
    {
        private readonly ITestOutputHelper _logger;

        public FplClientTests(ITestOutputHelper logger)
        {
            _logger = logger;
        }
        
        [Fact]
        public async Task GetStandings()
        {
            var client = new TryCatchFplClient(new FplClient());
            var standings = await client.GetStandings("579157");
            _logger.WriteLine(standings);
            Assert.NotEmpty(standings);
        }
        
        [Theory]
        [InlineData("salah")]
        [InlineData("mané")]
        [InlineData("firmino")]
        [InlineData("henderson")]
        [InlineData("wijnaldum")]
        [InlineData("tavares")]
        [InlineData("robertson")]
        [InlineData("van dijk")]
        [InlineData("matip")]
        [InlineData("trent")]
        [InlineData("alisson")]
        public async Task GetPlayer(string input)
        {
            var client = new TryCatchFplClient(new FplClient());
            var playerData = await client.GetAllFplDataForPlayer(input);
            _logger.WriteLine(playerData);
            Assert.NotEmpty(playerData);
        }
        
        [Theory]
        [InlineData("@fplbot player salah")]
        [InlineData("<@UREFQD887> player salah")]
        public async Task GetPlayerHandler(string input)
        {
            var client = new FplPlayerCommandHandler(new[] {new DummyPublisher(_logger)}, new FplClient());
            var playerData = await client.Handle(new SlackMessage
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
            var client = new FplPlayerCommandHandler(new[] {new DummyPublisher(_logger)}, new FplClient());
            var playerData = await client.Handle(new SlackMessage
            {
                Text = input,
                ChatHub = new SlackChatHub()
            });
            
            Assert.Equal("Not found", playerData.HandledMessage);
        }
    }

    public class DummyPublisher : IPublisher
    {
        private readonly ITestOutputHelper _helper;

        public DummyPublisher(ITestOutputHelper helper)
        {
            _helper = helper;
        }
        public Task Publish(Notification notification)
        {
            _helper.WriteLine(notification.Msg);
            return Task.CompletedTask;
        }
    }
}