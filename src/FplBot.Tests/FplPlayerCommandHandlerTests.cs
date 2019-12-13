using System;
using System.Threading.Tasks;
using FplBot.ConsoleApps.Handlers;
using FplBot.Tests.Helpers;
using Slackbot.Net.Workers.Handlers;
using SlackConnector.Models;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplPlayerCommandHandlerTests
    {
        private readonly IHandleMessages _client;

        public FplPlayerCommandHandlerTests(ITestOutputHelper logger)
        {
            _client = Factory.GetHandler<FplPlayerCommandHandler>(logger);
        }
        
        [Theory]
        [InlineData("@fplbot player salah")]
        [InlineData("<@UREFQD887> player salah")]
        public async Task GetPlayerHandler(string input)
        {
            var playerData = await _client.Handle(new SlackMessage
            {
                Text = input,
                ChatHub = new SlackChatHub()
            });
            
            Assert.Contains("Mohamed Salah", playerData.HandledMessage);
        }

        [Theory]
        [InlineData("@fplbot player ", "nonexistant")]
        [InlineData("<@UREFQD887> player ", "nonexistant")]
        public async Task GetPlayerHandlerNonPlayer(string input, string player)
        {
            var playerData = await _client.Handle(new SlackMessage
            {
                Text = $"{input}{player}",
                ChatHub = new SlackChatHub()
            });
            
            Assert.Equal("Fant ikke nonexistant", playerData.HandledMessage);
        }
        
        [Theory]
        [InlineData("@fplbot player ", "salah")]
        [InlineData("@fplbot player ", "mané")]
        [InlineData("@fplbot player ", "firmino")]
        [InlineData("@fplbot player ", "henderson")]
        [InlineData("@fplbot player ", "wijnaldum")]
        [InlineData("@fplbot player ", "tavares")]
        [InlineData("@fplbot player ", "robertson")]
        [InlineData("@fplbot player ", "van dijk")]
        [InlineData("@fplbot player ", "matip")]
        [InlineData("@fplbot player ", "trent")]
        [InlineData("@fplbot player ", "alisson")]
        public async Task GetPlayer(string input, string player)
        {
            var playerData = await _client.Handle(new SlackMessage
            {
                Text = $"{input}{player}",
                ChatHub = new SlackChatHub()
            });
            
            Assert.Contains(player, playerData.HandledMessage, StringComparison.InvariantCultureIgnoreCase);
        }
        
        [Theory]
        [InlineData("@fplbot player son")]
        public async Task GetMultipleResponse(string input)
        {
            var playerData = await _client.Handle(new SlackMessage
            {
                Text = input,
                ChatHub = new SlackChatHub()
            });
            
            Assert.Contains("Carl Jenkinson", playerData.HandledMessage);
            Assert.Contains("Harry Wilson", playerData.HandledMessage);
        }

       
    }
}