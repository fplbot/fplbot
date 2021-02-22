using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Fpl.Client
{
    public class FixtureClient : IFixtureClient
    {
        private readonly HttpClient _client;

        public FixtureClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<ICollection<Fixture>> GetFixtures()
        {
            var json = await _client.GetStringAsync("/api/fixtures/");

            return JsonConvert.DeserializeObject<ICollection<Fixture>>(json);
        }

        public async Task<ICollection<Fixture>> GetFixturesByGameweek(int id)
        {
            var json = await _client.GetStringAsync($"/api/fixtures/?event={id}");

            return JsonConvert.DeserializeObject<ICollection<Fixture>>(json);
        }
    }
}
