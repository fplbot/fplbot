using System;
using System.Threading.Tasks;

namespace FplBot.ConsoleApps.Clients
{
    public class TryCatchFplClient : IFplClient
    {
        private readonly IFplClient _client;

        public TryCatchFplClient(IFplClient client)
        {
            _client = client;
        }

        public async Task<string> GetAllFplDataForPlayer(string name)
        {
            try
            {
                return await _client.GetAllFplDataForPlayer(name);
            }
            catch (Exception e)
            {
                return $"Oops: {e.Message}";
            }
        }

        public async Task<string> GetStandings(string leagueId)
        {
            try
            {
                return await _client.GetStandings(leagueId);
            }
            catch (Exception e)
            {
                return $"Oops: {e.Message}";
            }
        }
    }
}