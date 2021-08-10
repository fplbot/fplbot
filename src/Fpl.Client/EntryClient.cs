using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fpl.Client
{
    public class EntryClient : IEntryClient
    {
        private readonly HttpClient _client;

        public EntryClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<BasicEntry> Get(int teamId, bool tolerate404 = false)
        {
            try
            {
                var json = await _client.GetStringAsync($"/api/entry/{teamId}/");

                return JsonConvert.DeserializeObject<BasicEntry>(json);
            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound && tolerate404)
            {
                return null;
            }
        }

        public async Task<EntryPicks> GetPicks(int teamId, int gameweek, bool tolerate404 = false)
        {
            try
            {
                var json = await _client.GetStringAsync($"/api/entry/{teamId}/event/{gameweek}/picks/");
                return JsonConvert.DeserializeObject<EntryPicks>(json);
            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound && tolerate404)
            {
                return null;
            }
        }
    }
}
