using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Slackbot.Net.SlackClients.Http.Configurations
{
    public class CommonHttpClientConfiguration
    {
        public static void ConfigureHttpClient(HttpClient c, string token)
        {
            ConfigureHttpClient(c);
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        
        public static void ConfigureHttpClient(HttpClient c)
        {
            c.BaseAddress = new Uri("https://slack.com/api/");
            c.Timeout = TimeSpan.FromSeconds(15);
        }
    }
}