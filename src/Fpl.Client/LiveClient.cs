using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fpl.Client
{
    public class LiveClient : ILiveClient
    {
        private readonly ICachedHttpClient _client;

        public LiveClient(ICachedHttpClient client)
        {
            _client = client;
        }

        public async Task<ICollection<LiveItem>> GetLiveItems(int gameweek, bool isOngoingGameweek = false)
        {
            var response = await _client.GetCachedOrFetch<LiveResponse>($"/api/event/{gameweek}/live/", isOngoingGameweek ? TimeSpan.FromMinutes(5) : TimeSpan.FromHours(24));
            return response.Elements;
        }
    }
}
