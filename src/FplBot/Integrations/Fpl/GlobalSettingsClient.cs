using Fpl.Client.Abstractions;
using Fpl.Client.Models;

namespace Fpl.Client;

public class GlobalSettingsClient(HttpClient httpClient, ICacheProvider client) : IGlobalSettingsClient
{
    public Task<GlobalSettings?> GetGlobalSettings()
    {
        return client.GetCachedOrFetch<GlobalSettings>("/api/bootstrap-static/",httpClient.GetStringAsync, TimeSpan.FromMinutes(5)); //max-age=300, stale-while-revalidate=1800, stale-if-error=3600
    }
}
