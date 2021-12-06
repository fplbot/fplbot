using Fpl.Client.Abstractions;
using Fpl.Client.Models;

namespace Fpl.Client;

public class GlobalSettingsClient : IGlobalSettingsClient
{
    private readonly HttpClient _httpClient;
    private readonly ICacheProvider _client;

    public GlobalSettingsClient(HttpClient httpClient, ICacheProvider client)
    {
        _httpClient = httpClient;
        _client = client;
    }

    public Task<GlobalSettings> GetGlobalSettings()
    {
        return _client.GetCachedOrFetch<GlobalSettings>("/api/bootstrap-static/",url => _httpClient.GetStringAsync(url), TimeSpan.FromMinutes(5)); //max-age=300, stale-while-revalidate=1800, stale-if-error=3600
    }
}
