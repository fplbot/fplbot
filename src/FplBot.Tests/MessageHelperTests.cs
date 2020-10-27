using System;
using Xunit;
using Xunit.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;

namespace FplBot.Tests
{
    public class MessageHelperTests
    {

        [Fact]
        public void ExtractGameweekShouldExtractCorrectGameweek()
        {
            var result = MessageHelper.ExtractGameweek("transfers 12", "transfers {gw}");
            Assert.Equal(12, result);
        }

        [Fact]
        public void ExtractArgsShouldExtractCorrectPlayerName()
        {
            var result = MessageHelper.ExtractArgs("player kane", "player {args}");
            Assert.Equal("kane", result);
        }

        [Fact]
        public void ExtractArgsShouldExtractCorrectListOfSubscriptionEvents()
        {
            var result = MessageHelper.ExtractArgs("subscribe standings, transfers, captains", "subscribe {args}");
            Assert.Equal("standings, transfers, captains", result);
        }

        [Fact]
        public void ExtractArgsShouldExtractCorrectListOfUnsubscriptionEvents()
        {
            var result = MessageHelper.ExtractArgs("unsubscribe standings, transfers, captains", "subscribe {args}");
            Assert.Equal("standings, transfers, captains", result);
        }
    }
}