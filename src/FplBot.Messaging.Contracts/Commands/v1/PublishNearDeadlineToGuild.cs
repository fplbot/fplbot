using NServiceBus;

namespace FplBot.Messaging.Contracts.Commands.v1
{
    public record PublishNearDeadlineToGuild(string GuildId, string Channel, int GameweekId) : ICommand;
}
