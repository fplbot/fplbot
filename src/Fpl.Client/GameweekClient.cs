using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fpl.Client
{
    public class GameweekClient : IGameweekClient
    {
        private readonly IGlobalSettingsClient _client;

        public GameweekClient(IGlobalSettingsClient client)
        {
            _client = client;
        }

        public async Task<ICollection<Gameweek>> GetGameweeks()
        {
            var globalSettings = await _client.GetGlobalSettings();

            return globalSettings.Gameweeks;
        }
    }
}
