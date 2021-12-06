using NServiceBus;

namespace FplBot.Messaging.Contracts.Commands.v1;

public record PublishStandingsToSlackWorkspace(string WorkspaceId, string Channel, int LeagueId, int GameweekId) : ICommand;
