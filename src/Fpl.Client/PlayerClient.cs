using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fpl.Client
{
    public class PlayerClient : IPlayerClient
    {
        private readonly ICachedHttpClient _client;
        private readonly IGlobalSettingsClient _globalSettingsClient;

        public PlayerClient(ICachedHttpClient client, IGlobalSettingsClient globalSettingsClient)
        {
            _client = client;
            _globalSettingsClient = globalSettingsClient;
        }

        public async Task<ICollection<Player>> GetAllPlayers()
        {
            var globalSettings = await _globalSettingsClient.GetGlobalSettings();

            return globalSettings.Players;
        }

        public Task<PlayerSummary> GetPlayer(int playerId)
        {
            return _client.GetCachedOrFetch<PlayerSummary>($"/api/element-summary/{playerId}/", TimeSpan.FromMinutes(60));
        }
    }
}
