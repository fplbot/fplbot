using System.Net.Http;
using System.Threading.Tasks;
using FplBot.Functions.Messaging.Internal;
using Microsoft.Extensions.Configuration;
using NServiceBus;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Configurations;

namespace FplBot.Functions
{
    public class PublishViaWebHookCommandHandler : IHandleMessages<PublishViaWebHook>
    {
        private readonly ISlackClient _client;

        public PublishViaWebHookCommandHandler(IConfiguration config, ISlackClientBuilder builder)
        {
            var token = config.GetValue<string>("SlackToken_FplBot_Workspace");
            _client = builder.Build(token);
        }

        public async Task Handle(PublishViaWebHook publish, IMessageHandlerContext context)
        {
            await _client.ChatPostMessage("#fplbot-notifications", publish.Message);
        }
    }
}
