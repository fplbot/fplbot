using System;
using System.Net.Http;
using FplBot.ConsoleApps.Clients;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace FplBot.ConsoleApps
{
    public class FplClientOptionsConfigurator : IConfigureNamedOptions<HttpClientFactoryOptions>
    {
        public void Configure(HttpClientFactoryOptions options)
        {
            
        }

        public void Configure(string name, HttpClientFactoryOptions options)
        {
            if(name is nameof(IFplClient))
                options.HttpClientActions.Add(SetupFplClient);
        }

        public static void SetupFplClient(HttpClient client)
        {
            client.BaseAddress = new Uri($"https://fantasy.premierleague.com");
        }
    }
}