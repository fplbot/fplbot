using System;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Slackbot.Net.SlackClients.Http.Configurations.Options;

namespace Slackbot.Net.SlackClients.Http.Configurations
{
    internal class SearchClientConfigurator : IConfigureNamedOptions<HttpClientFactoryOptions>
    {
        private readonly IOptions<OauthTokenClientOptions> _humanOptions;

        public SearchClientConfigurator(IOptions<OauthTokenClientOptions> humanOptions)
        {
            _humanOptions = humanOptions;

        }
        
        public void Configure(HttpClientFactoryOptions options)
        {
        }

        public void Configure(string name, HttpClientFactoryOptions options)
        {
            var token = "";

            if (name is nameof(SearchClient))
                token = _humanOptions.Value.OauthToken;
            
            if (name is nameof(SearchClient) && string.IsNullOrEmpty(token))
                throw new Exception("Missing token. Check configuration!");
            
            if (name is nameof(SearchClient))
            {
                options.HttpClientActions.Add(c => CommonHttpClientConfiguration.ConfigureHttpClient(c, token));

            }
            
        }
    }
}