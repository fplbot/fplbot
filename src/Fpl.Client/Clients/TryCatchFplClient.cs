using System;
using System.Threading.Tasks;
using Fpl.Client.Models;

namespace Fpl.Client.Clients
{
    public class TryCatchFplClient : IFplClient
    {
        private readonly IFplClient _client;

        public TryCatchFplClient(IFplClient client)
        {
            _client = client;
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

        public async Task<string> GetAllFplDataForPlayer(string name)
        {
            return await TryCatch(() =>_client.GetAllFplDataForPlayer(name));
        }
      

        private async Task<T> TryCatch<T>(Func<Task<T>> a) where T:class
        {
            try
            {
                return await a();
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}