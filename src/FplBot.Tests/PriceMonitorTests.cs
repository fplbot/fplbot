using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot.PriceMonitoring;
using Xunit;

namespace FplBot.Tests
{
    public class PriceMonitorTests
    {
        [Fact]
        public async Task GetChangedPlayers_WhenNoPlayers_ReturnsNoChanges()
        {
            var before = new List<Player>{ };
            var after = new List<Player>{ };

            var priceMonitor = CreatePriceMonitor(before, after);
            var priceChanges = await priceMonitor.GetChangedPlayers();
            Assert.Empty(priceChanges.Players);
            
            var secondPriceChangeCheck = await priceMonitor.GetChangedPlayers();
            Assert.Empty(secondPriceChangeCheck.Players);
        }
        
        [Fact]
        public async Task GetChangedPlayers_WhenSamePlayersWithPriceChange_ReturnsNoChanges()
        {
            var before = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(1) };
            var after = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(1) };

            var priceMonitor = CreatePriceMonitor(before, after);
            var priceChanges = await priceMonitor.GetChangedPlayers();
            Assert.Empty(priceChanges.Players);
            
            var secondPriceChangeCheck = await priceMonitor.GetChangedPlayers();
            Assert.Empty(secondPriceChangeCheck.Players);
        }
        
        [Fact]
        public async Task GetChangedPlayers_WhenSamePlayersWithChangeInPriceChange_ReturnsChanges()
        {
            var before = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(0) };
            var after = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(1) };

            var priceMonitor = CreatePriceMonitor(before, after);
            var priceChanges = await priceMonitor.GetChangedPlayers();
            Assert.Empty(priceChanges.Players);
            
            var secondCheck = await priceMonitor.GetChangedPlayers();
            Assert.NotEmpty(secondCheck.Players);
            Assert.Single(secondCheck.Players);
            Assert.Equal(TestBuilder.Player().Id, secondCheck.Players.First().Id);
        }
        
        [Fact]
        public async Task GetChangedPlayers_WhenSamePlayersWithChangeInPriceRemoved_ReturnsNoChanges()
        {
            var before = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(1) };
            var after = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(0) };

            var priceMonitor = CreatePriceMonitor(before, after);
            var priceChanges = await priceMonitor.GetChangedPlayers();
            Assert.Empty(priceChanges.Players);
            
            var secondCheck = await priceMonitor.GetChangedPlayers();
            Assert.Empty(secondCheck.Players);
        }
        
        [Fact]
        public async Task GetChangedPlayers_OneNewPriceChange_ReturnsChangesOnlyOnce()
        {
            var initial = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(1) };
            var firstCheck = new List<Player>
            {
                TestBuilder.Player().WithCostChangeEvent(1),
                TestBuilder.OtherPlayer().WithCostChangeEvent(1)
            };

            var priceMonitor = CreatePriceMonitor(initial, firstCheck, lastly: firstCheck);
            var initialCheck = await priceMonitor.GetChangedPlayers();
            Assert.Empty(initialCheck.Players);
            
            var priceChanges = await priceMonitor.GetChangedPlayers();
            Assert.NotEmpty(priceChanges.Players);
            Assert.Single(priceChanges.Players);
            Assert.Equal(TestBuilder.OtherPlayer().Id, priceChanges.Players.First().Id);
            
            var noChanges = await priceMonitor.GetChangedPlayers();
            Assert.Empty(noChanges.Players);
        }
        
        [Fact]
        public async Task GetChangedPlayers_OneLessPriceChange_ReturnsNoChanges()
        {
            var before = new List<Player>
            {
                TestBuilder.Player().WithCostChangeEvent(1),
                TestBuilder.OtherPlayer().WithCostChangeEvent(1)
            };
            
            var after = new List<Player>
            {
                TestBuilder.Player().WithCostChangeEvent(1)
            };

            var priceMonitor = CreatePriceMonitor(before, after);
            var initialCheck = await priceMonitor.GetChangedPlayers();
            Assert.Empty(initialCheck.Players);
            
            var priceChanges = await priceMonitor.GetChangedPlayers();
            Assert.Empty(priceChanges.Players);
        }
        
        [Fact]
        public async Task GetChangedPlayers_TwoIncreases_ReturnsBothChanges()
        {
            var initial = new List<Player>
            {
                TestBuilder.Player().WithCostChangeEvent(0),
            };
            
            var first = new List<Player>
            {
                TestBuilder.Player().WithCostChangeEvent(1),
            };
            
            var second = new List<Player>
            {
                TestBuilder.Player().WithCostChangeEvent(2),
            };
            
            var third = new List<Player>
            {
                TestBuilder.Player().WithCostChangeEvent(2),
            };

            var playerClient = A.Fake<IPlayerClient>();
            var teamsClient = A.Fake<ITeamsClient>();
            A.CallTo(() => playerClient.GetAllPlayers())
                .Returns(initial).Once()
                .Then.Returns(first).Once()
                .Then.Returns(second).Once()
                .Then.Returns(third);
            
            var priceMonitor = new PriceChangedMonitor(playerClient, teamsClient);
            
            var initialCheck = await priceMonitor.GetChangedPlayers();
            Assert.Empty(initialCheck.Players);
            
            var firstCheck = await priceMonitor.GetChangedPlayers();
            
            Assert.NotEmpty(firstCheck.Players);
            Assert.Single(firstCheck.Players);
            Assert.Equal(TestBuilder.Player().Id, firstCheck.Players.First().Id);
            Assert.Equal(1, firstCheck.Players.First().CostChangeEvent);
            
            var secondCheck = await priceMonitor.GetChangedPlayers();

            Assert.NotEmpty(secondCheck.Players);
            Assert.Single(secondCheck.Players);
            Assert.Equal(TestBuilder.Player().Id, secondCheck.Players.First().Id);
            Assert.Equal(2, secondCheck.Players.First().CostChangeEvent);
            
            var lastCheck = await priceMonitor.GetChangedPlayers();
            Assert.Empty(lastCheck.Players);
        }

        private static PriceChangedMonitor CreatePriceMonitor(ICollection<Player> before, ICollection<Player> after, ICollection<Player> lastly = null)
        {
            var playerClient = A.Fake<IPlayerClient>();
            var teamsClient = A.Fake<ITeamsClient>();
            A.CallTo(() => playerClient.GetAllPlayers())
                .Returns(before).Once()
                .Then.Returns(after).Once()
                .Then.Returns(lastly);
            return new PriceChangedMonitor(playerClient, teamsClient);
        }
    }
}