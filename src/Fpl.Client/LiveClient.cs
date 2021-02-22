using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fpl.Client
{
    public class LiveClient : ILiveClient
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheProvider _client;

        public LiveClient(HttpClient httpClient,ICacheProvider client)
        {
            _httpClient = httpClient;
            _client = client;
        }

        public async Task<ICollection<LiveItem>> GetLiveItems(int gameweek, bool isOngoingGameweek = false)
        {
            var response = await _client.GetCachedOrFetch<LiveResponse>($"/api/event/{gameweek}/live/", url => _httpClient.GetStringAsync(url), isOngoingGameweek ? TimeSpan.FromMinutes(5) : TimeSpan.FromHours(24));
            return response.Elements;
        }
    }
}
