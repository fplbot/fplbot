using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fpl.Client
{
    public class PlayerClient : IPlayerClient
    {
        private readonly IGlobalSettingsClient _globalSettingsClient;

        public PlayerClient(IGlobalSettingsClient globalSettingsClient)
        {
            _globalSettingsClient = globalSettingsClient;
        }

        public async Task<ICollection<Player>> GetAllPlayers()
        {
            var globalSettings = await _globalSettingsClient.GetGlobalSettings();
            return globalSettings.Players;
        }
    }
}
