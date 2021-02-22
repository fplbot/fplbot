using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fpl.Client
{
    public class TeamsClient : ITeamsClient
    {
        private readonly IGlobalSettingsClient _globalSettingsClient;

        public TeamsClient(IGlobalSettingsClient globalSettingsClient)
        {
            _globalSettingsClient = globalSettingsClient;
        }

        public async Task<ICollection<Team>> GetAllTeams()
        {
            var globalSettings = await _globalSettingsClient.GetGlobalSettings();

            return globalSettings.Teams;
        }

        public async Task<Team> GetTeam(int teamId)
        {
            var teams = await GetAllTeams();
            return teams.FirstOrDefault(t => t.Id == teamId);
        }
    }
}
