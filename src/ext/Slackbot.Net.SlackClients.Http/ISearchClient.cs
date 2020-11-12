using System.Threading.Tasks;
using Slackbot.Net.SlackClients.Http.Models.Responses.SearchMessages;

namespace Slackbot.Net.SlackClients.Http
{
    /// <summary>
    /// A slack client for endpoints that require a OAuth token (a human user credentials)
    /// </summary>
    public interface ISearchClient
    {
        /// <summary>
        /// Search the message history of the user connected to the OAuth token
        /// Scopes required: search:read 
        /// </summary>
        /// <param name="query">search query</param>
        /// <remarks>https://api.slack.com/methods/search.messages</remarks>
        Task<SearchMessagesResponse> SearchMessagesAsync(string query);
    }
}