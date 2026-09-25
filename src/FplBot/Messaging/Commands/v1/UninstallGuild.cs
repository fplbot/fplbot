using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Messaging.Contracts.Commands.v1;

public record UninstallGuild(string GuildId, UninstallReason Reason);
