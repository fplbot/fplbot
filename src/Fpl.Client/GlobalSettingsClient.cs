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
            var json = await _client.GetStringAsync("/api/bootstrap-static/");

            return JsonConvert.DeserializeObject<GlobalSettings>(json);
        }
    }
}
