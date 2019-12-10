using System;
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
        public void Configure(HttpClientFactoryOptions options)
        {
            
        }

        public void Configure(string name, HttpClientFactoryOptions options)
        {
            if (name is nameof(IFplClient))
            {
                options.HttpClientActions.Add(SetupFplClient);
                options.HttpMessageHandlerBuilderActions.Add(b => ConfigurePrimaryHandler(b.PrimaryHandler as HttpClientHandler));
            }
        }

        public static void ConfigurePrimaryHandler(HttpClientHandler handler)
        {
            handler.AutomaticDecompression = DecompressionMethods.GZip;
            handler.SslProtocols = SslProtocols.Tls12;       
        }

        public static void SetupFplClient(HttpClient client)
        {
            client.BaseAddress = new Uri($"https://fantasy.premierleague.com");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            client.DefaultRequestHeaders.Add("User-Agent", "Lol");
        }
    }
}