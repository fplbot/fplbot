using System.Net.Http;
using System.Threading.Tasks;
using FplBot.Functions.Messaging.Internal;
using Microsoft.Extensions.Configuration;
using NServiceBus;

namespace FplBot.Functions
{
    public class PublishViaWebHookCommandHandler : IHandleMessages<PublishViaWebHook>
    {
        private readonly string _webhookUrl;
        private static readonly HttpClient HttpClient = new HttpClient();


        public PublishViaWebHookCommandHandler(IConfiguration config)
        {
            _webhookUrl = config.GetValue<string>("SlackWebHookUrl");
        }
        
        public async Task Handle(PublishViaWebHook publish, IMessageHandlerContext context)
        {
            await HttpClient.PostAsJsonAsync(_webhookUrl, new
            {
                channel = "#johntester",
                text = publish.Message
            });
        }
    }
}