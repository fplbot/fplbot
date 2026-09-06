using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Messaging.Contracts.Commands.v1;

public record PublishFixtureEventsToGuild(string GuildId, string ChannelId, List<FixtureEvents> FixtureEvents);
