using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;


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
            return await _client.GetFromJsonAsync<ICollection<Fixture>>("/api/fixtures/", JsonConvert.JsonSerializerOptions);
        }

        public async Task<ICollection<Fixture>> GetFixturesByGameweek(int id)
        {
            return await _client.GetFromJsonAsync<ICollection<Fixture>>($"/api/fixtures/?event={id}", JsonConvert.JsonSerializerOptions);
        }
    }
}
