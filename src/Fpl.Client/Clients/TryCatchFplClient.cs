using System;
using System.Threading.Tasks;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;

namespace Fpl.Client.Clients
{
    public class TryCatchFplClient : IFplClient
    {
        private readonly IFplClient _client;
        private readonly ILogger<TryCatchFplClient> _logger;

        public TryCatchFplClient(IFplClient client, ILogger<TryCatchFplClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<PlayerStats> GetPlayerData(string playerId)
        {
            return await TryCatch(() =>_client.GetPlayerData(playerId));
        }

        public async Task<ScoreBoard> GetScoreBoard(string leagueId)
        {
            return await TryCatch(() =>_client.GetScoreBoard(leagueId));
        }

        public async Task<Bootstrap> GetBootstrap()
        {
            return await TryCatch(() =>_client.GetBootstrap());
        }

        private async Task<T> TryCatch<T>(Func<Task<T>> a) where T:class
        {
            try
            {
                return await a();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
    }
}