using System;
using System.Threading.Tasks;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace Slackbot.Net.Extensions.FplBot.NServiceBusHandlers.Sagas
{
    public class TrendingPlayerSaga : Saga<TrendingPlayerSagaData>, IAmStartedByMessages<PlayerTransfersUpdated>
    {
        private readonly ILogger<TrendingPlayerSaga> _logger;

        public TrendingPlayerSaga(ILogger<TrendingPlayerSaga> logger)
        {
            _logger = logger;
        }
        
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TrendingPlayerSagaData> mapper)
        {
            mapper.MapSaga(s => s.PlayerId).ToMessage<PlayerTransfersUpdated>(m => m.PlayerId);
        }
        
        public async Task Handle(PlayerTransfersUpdated message, IMessageHandlerContext context)
        {
            Console.WriteLine($"{Data})");
            Console.WriteLine($"{message}");

            decimal changeInRatio = (message.TransfersToOwnersRatio - Data.TransfersToOwnersRatio);
            var isChange = changeInRatio != 0;

            if (isChange)
            {
                Data.TrendCount++;
            }

            if (message.TransfersToOwnersRatio >= 0 && changeInRatio <= 0)
            {
                Console.WriteLine("RESETTING TRENDCOUNT +");
                Data.TrendCount = 0;
            }
            if (message.TransfersToOwnersRatio <= 0 && changeInRatio >= 0)
            {
                Console.WriteLine("RESETTING TRENDCOUNT -");
                Data.TrendCount = 0;
            }
            

            if (isChange && Data.TrendCount > 0)
            {
                await context.Publish(new PlayerTrending(message.PlayerId, message.PlayerName, Data.TrendCount, message.TransfersToOwnersRatio));
                Console.WriteLine($"#{Data.TrendCount}. Change for {message.PlayerName} ({message.PlayerId}). message.TransfersToOwnersRatio: {message.TransfersToOwnersRatio}");
            }
       
            else
            {
                Console.WriteLine($"#{Data.TrendCount}. NO change for {message.PlayerName} ({message.PlayerId}). message.TransfersToOwnersRatio: {message.TransfersToOwnersRatio}");
            }

            Data.TransfersToOwnersRatio = message.TransfersToOwnersRatio;
        }
    }


    public class TrendingPlayerSagaData : ContainSagaData
    {
        public int PlayerId { get; set; }
        public decimal TransfersToOwnersRatio { get; set; }
        public int TrendCount { get; set; }
    }
}