using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Slackbot.Net.SlackClients.Http.Extensions;
using Slackbot.Net.SlackClients.Http.Models.Requests.OAuthAccess;
using Slackbot.Net.SlackClients.Http.Models.Responses.OAuthAccess;

namespace Slackbot.Net.SlackClients.Http
{
    internal class SlackOAuthAccessClient : ISlackOAuthAccessClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<SlackOAuthAccessClient> _logger;

        public SlackOAuthAccessClient(HttpClient client, ILogger<SlackOAuthAccessClient> logger)
        {
            _client = client;
            _logger = logger;
        }
        
        /// <inheritdoc/>
        public async Task<OAuthAccessResponse> OAuthAccess(OauthAccessRequest oauthAccessRequest)
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", oauthAccessRequest.ClientId),
                new KeyValuePair<string, string>("client_secret", oauthAccessRequest.ClientSecret),
                new KeyValuePair<string, string>("code", oauthAccessRequest.Code)
            };

            if (!string.IsNullOrEmpty(oauthAccessRequest.RedirectUri))
            {
                parameters.Add(new KeyValuePair<string, string>("redirect_uri", oauthAccessRequest.RedirectUri));
            }
            
            if (oauthAccessRequest.SingleChannel.HasValue)
            {
                parameters.Add(new KeyValuePair<string, string>("single_channel", oauthAccessRequest.SingleChannel.Value ? "true" : "false"));
            }

            return await _client.PostParametersAsForm<OAuthAccessResponse>(parameters,"oauth.access", s => _logger?.LogTrace(s));
        }
        
        /// <inheritdoc/>
        public async Task<OAuthAccessResponseV2> OAuthAccessV2(OauthAccessRequestV2 oauthAccessRequest)
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("code", oauthAccessRequest.Code)
            };

            if (!string.IsNullOrEmpty(oauthAccessRequest.RedirectUri))
            {
                parameters.Add(new KeyValuePair<string, string>("redirect_uri", oauthAccessRequest.RedirectUri));
            }

            if (!string.IsNullOrEmpty(oauthAccessRequest.ClientId))
            {
                parameters.Add(new KeyValuePair<string, string>("client_id", oauthAccessRequest.ClientId));
            }

            if (!string.IsNullOrEmpty(oauthAccessRequest.ClientSecret))
            {
                parameters.Add(new KeyValuePair<string, string>("client_secret", oauthAccessRequest.ClientSecret));
            }
            
      

            return await _client.PostParametersAsForm<OAuthAccessResponseV2>(parameters,"oauth.v2.access", s => _logger?.LogTrace(s));
        }
    }
}