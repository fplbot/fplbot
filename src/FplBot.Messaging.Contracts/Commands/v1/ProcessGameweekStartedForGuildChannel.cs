using NServiceBus;

namespace FplBot.Messaging.Contracts.Commands.v1;

public record ProcessGameweekStartedForGuildChannel(string GuildId, string ChannelId, int GameweekId) : ICommand;