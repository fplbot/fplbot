using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Xunit;

namespace FplBot.Tests
{
    public class PriceMonitorTests
    {
        [Fact]
        public void GetChangedPlayers_WhenNoPlayers_ReturnsNoChanges()
        {
            var before = new List<Player>{ };
            var after = new List<Player>{ };
            
            var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after,before, new List<Team>());
            
            Assert.Empty(priceChanges);
        }
        
        [Fact]
        public void GetChangedPlayers_WhenSamePlayersWithPriceChange_ReturnsNoChanges()
        {
            var before = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(1) };
            var after = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(1) };

            var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after,before, new List<Team>());
            
            Assert.Empty(priceChanges);
        }
        
        [Fact]
        public void GetChangedPlayers_WhenSamePlayersWithChangeInPriceChange_ReturnsChanges()
        {
            var before = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(0) };
            var after = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(1) };
            
            var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after,before, new List<Team>());

            Assert.Single(priceChanges);
            Assert.Equal(TestBuilder.Player().SecondName, priceChanges.First().PlayerSecondName);
        }
        
        [Fact]
        public void GetChangedPlayers_WhenSamePlayersWithChangeInPriceRemoved_ReturnsNoChanges()
        {
            var before = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(1) };
            var after = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(0) };
            
            var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after,before, new List<Team>());
            
            Assert.Empty(priceChanges);
        }
        
        [Fact]
        public void GetChangedPlayers_OneNewPlayerWithCostChange_ReturnsNewPlayer()
        {
            var before = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(1) };
            var after = new List<Player>
            {
                TestBuilder.Player().WithCostChangeEvent(1),
                TestBuilder.OtherPlayer().WithCostChangeEvent(1)
            };

            var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after, before, new List<Team>());

            Assert.Single(priceChanges);
            Assert.Equal(TestBuilder.OtherPlayer().SecondName, priceChanges.First().PlayerSecondName);
        }
        
        [Fact]
        public void GetChangedPlayers_OnePlayerRemoved_ReturnsNoChanges()
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

            var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after, before, new List<Team>());

            Assert.Empty(priceChanges);
        }
    }
}