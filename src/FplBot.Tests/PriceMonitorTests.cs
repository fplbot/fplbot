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
        public async Task GetChangedPlayers_WhenCalledOnce_ReturnsNoChanges()
        {
            var before = new List<Player>{ TestBuilder.Player().OfCost(10) };
            var after = new List<Player>{ TestBuilder.Player().OfCost(10) };

            var priceMonitor = CreatePriceMonitor(before, after);
            var priceChanges = await priceMonitor.GetChangedPlayers();
            Assert.Empty(priceChanges.Players);
        }

        [Fact]
        public async Task GetChangedPlayers_WhenCalledTwice_AndChanges_ReturnsChanges()
        {
            var before = new List<Player>{ TestBuilder.Player().OfCost(10) };
            var after = new List<Player>{ TestBuilder.Player().OfCost(11) };

            var priceMonitor = CreatePriceMonitor(before, after);
            var initialCheck = await priceMonitor.GetChangedPlayers();
            Assert.Empty(initialCheck.Players);
            
            var priceChanges = await priceMonitor.GetChangedPlayers();
            Assert.NotEmpty(priceChanges.Players);
            Assert.Single(priceChanges.Players);
        }

        [Fact]
        public async Task PriceDecrease_ReturnsPriceChangedPlayers()
        {
            var before = new List<Player>{ TestBuilder.Player().OfCost(10) };
            var after = new List<Player>{ TestBuilder.Player().OfCost(9) };

            var priceMonitor = CreatePriceMonitor(before, after);
            var initialCheck = await priceMonitor.GetChangedPlayers();
            Assert.Empty(initialCheck.Players);
            
            var priceChanges = await priceMonitor.GetChangedPlayers();
            Assert.NotEmpty(priceChanges.Players);
            Assert.Single(priceChanges.Players);
        }
        
        [Fact]
        public async Task PriceIncrease_FirstReturnsInitialNoChange_ThenChanges_ThenNoChanges()
        {
            var before = new List<Player>{ TestBuilder.Player().OfCost(10) };
            var after = new List<Player>{ TestBuilder.Player().OfCost(9) };

            var priceMonitor = CreatePriceMonitor(before, after);
            var initialCheck = await priceMonitor.GetChangedPlayers();
            Assert.Empty(initialCheck.Players);
            
            var priceChanges = await priceMonitor.GetChangedPlayers();
            Assert.NotEmpty(priceChanges.Players);
            Assert.Single(priceChanges.Players);
            
            var noChanges = await priceMonitor.GetChangedPlayers();
            Assert.Empty(noChanges.Players);
        }

        private static PriceChangedMonitor CreatePriceMonitor(ICollection<Player> before, ICollection<Player> after)
        {
            var playerClient = A.Fake<IPlayerClient>();
            A.CallTo(() => playerClient.GetAllPlayers()).Returns(before).Once().Then.Returns(after);
            return new PriceChangedMonitor(playerClient);
        }
    }
}