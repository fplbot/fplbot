using System.Net.Http;
using System.Threading.Tasks;
using FplBot.Functions.Messaging.Internal;
using Microsoft.Extensions.Configuration;
using NServiceBus;
using Slackbot.Net.SlackClients.Http.Configurations;

namespace FplBot.Functions
{
    public class PublishViaWebHookCommandHandler : IHandleMessages<PublishViaWebHook>
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public PublishViaWebHookCommandHandler(IConfiguration config)
        {
            var token = config.GetValue<string>("SlackToken_FplBot_Workspace");
            CommonHttpClientConfiguration.ConfigureHttpClient(HttpClient, token);
        }

        public async Task Handle(PublishViaWebHook publish, IMessageHandlerContext context)
        {
            await HttpClient.PostAsJsonAsync("/chat.postMessage", new
            {
                channel = "#fplbot-notifications",
                text = publish.Message
            });
        }
    }
}
