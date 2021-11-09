using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.Messaging.Contracts.Commands.v1;

public record PublishFixtureEventsToSlackWorkspace(string WorkspaceId, List<FixtureEvents> FixtureEvents) : ICommand;