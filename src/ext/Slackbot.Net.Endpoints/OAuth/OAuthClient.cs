using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Slackbot.Net.Endpoints.OAuth
{
    internal record Response(bool Ok, string Error);

    internal class OAuthClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<OAuthClient> _logger;

        internal record OauthAccessV2Request(string Code, string ClientId, string ClientSecret, string RedirectUri);

        internal record OAuthAccessV2Response(string Access_Token, string Scope, Team Team, string App_Id, OAuthUser Authed_User, bool Ok, string Error) : Response(Ok, Error);
        internal record Team(string Id, string Name);
        internal record OAuthUser(string User_Id, string App_Home);


        public OAuthClient(HttpClient client, ILogger<OAuthClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<OAuthAccessV2Response> OAuthAccessV2(OauthAccessV2Request oauthAccessRequest)
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                new("code", oauthAccessRequest.Code)
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

            return await _client.PostParametersAsForm<OAuthAccessV2Response>(parameters,"oauth.v2.access", s => _logger.LogInformation(s));
        }
    }

    internal static class HttpClientExtensions
    {
        internal static async Task<T> PostParametersAsForm<T>(this HttpClient httpClient, IEnumerable<KeyValuePair<string, string>> parameters, string api, Action<string> logger = null) where T: Response
        {
            var request = new HttpRequestMessage(HttpMethod.Post, api);

            if (parameters != null && parameters.Any())
            {
                var formUrlEncodedContent = new FormUrlEncodedContent(parameters);
                var requestContent = await formUrlEncodedContent.ReadAsStringAsync();
                var httpContent = new StringContent(requestContent, Encoding.UTF8, "application/x-www-form-urlencoded");
                httpContent.Headers.ContentType.CharSet = string.Empty;
                request.Content = httpContent;
            }

            var response =  await httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger?.Invoke($"{response.StatusCode} \n {responseContent}");
            }

            response.EnsureSuccessStatusCode();

            var resObj = JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if(!resObj.Ok)
                throw new SlackApiException(error: $"{resObj.Error}", responseContent:responseContent);

            return resObj;
        }
    }

    internal class SlackApiException : Exception
    {
        public string Error { get; }
        public string ResponseContent { get; }

        public SlackApiException(string error, string responseContent): base(responseContent)
        {
            Error = error;
            ResponseContent = responseContent;
        }
    }


}
