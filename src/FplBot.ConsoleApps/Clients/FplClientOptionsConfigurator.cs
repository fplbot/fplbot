using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using FplBot.ConsoleApps.Clients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace FplBot.ConsoleApps
{
    public class FplClientOptionsConfigurator : IConfigureNamedOptions<HttpClientFactoryOptions>
    {
        private readonly FplHttpHandler _fplHttphandler;

        public FplClientOptionsConfigurator(FplHttpHandler fplHttphandler)
        {
            _fplHttphandler = fplHttphandler;
        }
        
        public void Configure(HttpClientFactoryOptions options)
        {
            
        }

        public void Configure(string name, HttpClientFactoryOptions options)
        {
            if (name is nameof(IFplClient))
            {
                options.HttpClientActions.Add(SetupFplClient);
                options.HttpMessageHandlerBuilderActions.Add(b =>
                {
                    b.PrimaryHandler = _fplHttphandler;
                });
            }
        }

        public static void SetupFplClient(HttpClient client)
        {
            client.BaseAddress = new Uri($"https://fantasy.premierleague.com");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            client.DefaultRequestHeaders.Add("User-Agent", "Lol");
        }
    }
}