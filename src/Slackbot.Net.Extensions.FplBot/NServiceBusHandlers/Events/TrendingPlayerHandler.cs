using System;
using System.Threading;
using System.Threading.Tasks;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace Slackbot.Net.Extensions.FplBot.NServiceBusHandlers.Events
{
    public class TrendingPlayerHandler : IHandleMessages<PlayerTrending>
    {
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ILogger<TrendingPlayerHandler> _logger;

        public TrendingPlayerHandler(ISlackWorkSpacePublisher publisher, ILogger<TrendingPlayerHandler> logger)
        {
            _publisher = publisher;
            _logger = logger;
        }
        
        public async Task Handle(PlayerTrending message, IMessageHandlerContext context)
        {
            var chartMoji = message.TransfersToOwnersRatio > 0 ? "📈" : message.TransfersToOwnersRatio < 0 ? "📉" : "⁉️";
            var trendCount = $"{chartMoji}x{message.TrendCount}";
            var trendverb = message.TransfersToOwnersRatio > 0 ? "trending!" : message.TransfersToOwnersRatio < 0 ? "losing owners fast.." : "NOT REALLY TRENDING!";
            string additional = "";

            if (message.TransfersToOwnersRatio > 0)
            {
                additional = message.TrendCount == 1 ? "" : message.TrendCount == 2 ? "Again!" : message.TrendCount == 3 ? "FOMO?!" : "🚀🚀🚀";
            }

            if (message.TransfersToOwnersRatio < 0)
            {
                additional = message.TrendCount == 1 ? "" : message.TrendCount == 2 ? "Ouch! Going down.." : message.TrendCount == 3 ? "Maybe time to sell?" : "⚰️⚰️⚰️";
            }
                
            string messages = $"{trendCount} {message.PlayerName} is {trendverb} {additional}";
            Console.WriteLine(messages);
            await _publisher.PublishToWorkspace("T0A9QSU83", "#johntest", messages);
        }
    }
}