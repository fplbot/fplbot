using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Newtonsoft.Json;

namespace Fpl.Client
{
    public class TeamsClient : ITeamsClient
    {
        private readonly HttpClient _client;

        public TeamsClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<ICollection<Team>> GetAllTeams()
        {
            var json = await _client.GetStringAsync("/api/bootstrap-static/");

            var data = JsonConvert.DeserializeObject<GlobalSettings>(json);

            return data.Teams;
        }

        public async Task<Team> GetTeam(int teamId)
        {
            var teams = await GetAllTeams();
            return teams.FirstOrDefault(t => t.Id == teamId);
        }
    }
}
