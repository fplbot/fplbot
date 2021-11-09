using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.Messaging.Contracts.Commands.v1;

public record PublishPriceChangesToSlackWorkspace(string WorkspaceId, List<PlayerWithPriceChange> PlayersWithPriceChanges) : ICommand;