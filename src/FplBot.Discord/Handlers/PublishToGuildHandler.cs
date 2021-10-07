using System.Threading.Tasks;
using Discord.Net.HttpClients;
using FplBot.Messaging.Contracts.Commands.v1;
using NServiceBus;

namespace FplBot.Discord.Handlers
{
    public class PublishToGuildHandler : IHandleMessages<PublishToGuildChannel>
    {
        private readonly DiscordClient _discordClient;

        public PublishToGuildHandler(DiscordClient discordClient)
        {
            _discordClient = discordClient;
        }

        public async Task Handle(PublishToGuildChannel message, IMessageHandlerContext context)
        {
            await _discordClient.ChannelMessagePost(message.ChannelId, message.Message);
        }
    }
}
