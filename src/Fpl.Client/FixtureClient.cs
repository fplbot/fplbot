using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
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
           const string url = "/api/fixtures/";

            var json = await _client.GetStringAsync(url);

            return JsonConvert.DeserializeObject<ICollection<Fixture>>(json);
        }

        public async Task<ICollection<Fixture>> GetFixturesByGameweek(int id)
        {
           var url = $"/api/fixtures/?event={id}";

            var json = await _client.GetStringAsync(url);

            return JsonConvert.DeserializeObject<ICollection<Fixture>>(json);
        }
    }
}
