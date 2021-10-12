using System.Threading.Tasks;
using Discord.Net.HttpClients;
using FplBot.Messaging.Contracts.Commands.v1;
using NServiceBus;

namespace FplBot.Discord.Handlers
{
    public class PublishToGuildHandler :
        IHandleMessages<PublishToGuildChannel>,
        IHandleMessages<PublishRichToGuildChannel>
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

        public async Task Handle(PublishRichToGuildChannel message, IMessageHandlerContext context)
        {
            await _discordClient.ChannelMessagePost(message.ChannelId, new DiscordClient.RichEmbed(message.Title, message.Description));
        }
    }
}
