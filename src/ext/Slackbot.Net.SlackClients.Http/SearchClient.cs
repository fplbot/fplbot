using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Slackbot.Net.SlackClients.Http.Extensions;
using Slackbot.Net.SlackClients.Http.Models.Responses.SearchMessages;

namespace Slackbot.Net.SlackClients.Http
{
    /// <inheritdoc/>

    internal class SearchClient : ISearchClient
    {
        private readonly HttpClient _httpClient;

        public SearchClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <inheritdoc/>
        public async Task<SearchMessagesResponse> SearchMessagesAsync(string query)
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("query", query),
                new KeyValuePair<string, string>("sort", "timestamp"),
                new KeyValuePair<string, string>("count", "1"),
                new KeyValuePair<string, string>("sort_dir", "asc")
            };
            return await _httpClient.PostParametersAsForm<SearchMessagesResponse>(parameters, "search.messages");
        }
    }
}