using NServiceBus;

namespace FplBot.Messaging.Contracts.Commands.v1
{
    public record PublishGameweekFinishedToGuild(string GuildId, string ChannelId, int? LeagueId, int GameweekId) : ICommand;
}
