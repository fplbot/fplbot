using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Messaging.Contracts.Commands.v1;

public record PublishFixtureEventsToGuild(string TeamId, string ChannelId, List<FixtureEvents> FixtureEvents);
