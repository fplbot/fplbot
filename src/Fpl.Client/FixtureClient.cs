using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fpl.Client
{
    public class FixtureClient : IFixtureClient
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheProvider _client;

        public FixtureClient(HttpClient httpClient, ICacheProvider client)
        {
            _httpClient = httpClient;
            _client = client;
        }

        public Task<ICollection<Fixture>> GetFixtures()
        {
            return _client.GetCachedOrFetch<ICollection<Fixture>>("/api/fixtures/", url => _httpClient.GetStringAsync(url),  TimeSpan.FromMinutes(5));
        }

        public Task<ICollection<Fixture>> GetFixturesByGameweek(int id)
        {
            return _client.GetCachedOrFetch<ICollection<Fixture>>($"/api/fixtures/?event={id}", url => _httpClient.GetStringAsync(url), TimeSpan.FromMinutes(5));
        }
    }
}
