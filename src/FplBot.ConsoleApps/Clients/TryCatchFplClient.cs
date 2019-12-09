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
            return await TryCatch(() =>_client.GetAllFplDataForPlayer(name));
        }

        public async Task<string> GetStandings(string leagueId)
        {
            return await TryCatch(() =>_client.GetStandings(leagueId));
        }

        private async Task<string> TryCatch(Func<Task<string>> a)
        {
            try
            {
                return await a();
            }
            catch (Exception e)
            {
                return $"Oops: {e.Message}";
            }
        }
    }
}