using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fpl.Client
{
    public class FixtureClient : IFixtureClient
    {
        private readonly ICachedHttpClient _client;

        public FixtureClient(ICachedHttpClient client)
        {
            _client = client;
        }

        public Task<ICollection<Fixture>> GetFixtures()
        {
            return _client.GetCachedOrFetch<ICollection<Fixture>>("/api/fixtures/", TimeSpan.FromMinutes(5));
        }

        public Task<ICollection<Fixture>> GetFixturesByGameweek(int id)
        {
            return _client.GetCachedOrFetch<ICollection<Fixture>>($"/api/fixtures/?event={id}", TimeSpan.FromMinutes(5));
        }
    }
}
