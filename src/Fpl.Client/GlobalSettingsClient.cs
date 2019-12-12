using System.Net.Http;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Newtonsoft.Json;

namespace Fpl.Client
{
    public class GlobalSettingsClient : IGlobalSettingsClient
    {
        private readonly HttpClient _client;

        public GlobalSettingsClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<GlobalSettings> GetGlobalSettings()
        {
           const string url = "/api/bootstrap-static/";

            var json = await _client.GetStringAsync(url);

            return JsonConvert.DeserializeObject<GlobalSettings>(json);
        }
    }
}
