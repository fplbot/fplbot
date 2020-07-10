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
        public async Task GetChangedPlayers_OneNewPriceChange_ReturnsChangesOnlyOnce()
        {
            var before = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(1) };
            var after = new List<Player>
            {
                TestBuilder.Player().WithCostChangeEvent(1),
                TestBuilder.OtherPlayer().WithCostChangeEvent(1)
            };

            var priceMonitor = CreatePriceMonitor(before, after);
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

        private static PriceChangedMonitor CreatePriceMonitor(ICollection<Player> before, ICollection<Player> after)
        {
            var playerClient = A.Fake<IPlayerClient>();
            A.CallTo(() => playerClient.GetAllPlayers()).Returns(before).Once().Then.Returns(after);
            return new PriceChangedMonitor(playerClient);
        }
    }
}