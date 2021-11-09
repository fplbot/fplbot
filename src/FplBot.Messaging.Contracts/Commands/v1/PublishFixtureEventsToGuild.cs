using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.Messaging.Contracts.Commands.v1;

public record PublishFixtureEventsToGuild(string GuildId, string ChannelId, List<FixtureEvents> FixtureEvents) : ICommand;