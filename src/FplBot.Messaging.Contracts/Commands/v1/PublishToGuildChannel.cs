using NServiceBus;

namespace FplBot.Messaging.Contracts.Commands.v1
{
    public record PublishToGuildChannel(string GuildId, string ChannelId, string Message) : ICommand;
}
