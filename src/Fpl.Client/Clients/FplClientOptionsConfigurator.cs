using System;
using System.Net;
using System.Net.Http;
using Fpl.Client.Abstractions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Fpl.Client.Clients
{
    public class FplClientOptionsConfigurator : IConfigureNamedOptions<HttpClientFactoryOptions>
    {
        public void Configure(HttpClientFactoryOptions options)
        {
            
        }

        public void Configure(string name, HttpClientFactoryOptions options)
        {
            if (IsOneOf(name))
            {
                options.HttpClientActions.Add(SetupFplClient);
                options.HttpMessageHandlerBuilderActions.Add(b =>
                    b.PrimaryHandler = new HttpClientHandler
                    {
                        AutomaticDecompression = DecompressionMethods.GZip,
                        SslProtocols = System.Security.Authentication.SslProtocols.Tls12
                    });
            }
        }

        private static bool IsOneOf(string name)
        {
            return name is nameof(IEntryClient) ||
                   name is nameof(IEntryHistoryClient) ||
                   name is nameof(IFixtureClient) ||
                   name is nameof(IGameweekClient) ||
                   name is nameof(ILeagueClient) ||
                   name is nameof(ITeamsClient) ||
                   name is nameof(IPlayerClient) ||
                   name is nameof(ITransfersClient);
        }

        public static void SetupFplClient(HttpClient client)
        {
            client.BaseAddress = new Uri($"https://fantasy.premierleague.com");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            client.DefaultRequestHeaders.Add("User-Agent", "Lol");
        }
    }
}