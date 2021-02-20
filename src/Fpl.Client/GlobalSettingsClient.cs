using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using System;
using System.Threading.Tasks;

namespace Fpl.Client
{
    public class GlobalSettingsClient : IGlobalSettingsClient
    {
        private readonly ICachedHttpClient _client;

        public GlobalSettingsClient(ICachedHttpClient client)
        {
            _client = client;
        }

        public Task<GlobalSettings> GetGlobalSettings()
        {
            return _client.GetCachedOrFetch<GlobalSettings>("/api/bootstrap-static/", TimeSpan.FromHours(24));
        }
    }
}