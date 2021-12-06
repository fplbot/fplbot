using NServiceBus;

namespace FplBot.Messaging.Contracts.Commands.v1;

public record ProcessGameweekStartedForSlackWorkspace(string WorkspaceId, int GameweekId) : ICommand;
