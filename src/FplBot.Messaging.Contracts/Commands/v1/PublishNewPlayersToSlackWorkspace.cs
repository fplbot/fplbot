using System.Collections.Generic;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.Messaging.Contracts.Commands.v1
{
    public class PublishNewPlayersToSlackWorkspace : ICommand
    {
        public IEnumerable<NewPlayer> NewPlayers { get; }
        public string WorkspaceId { get; }

        public PublishNewPlayersToSlackWorkspace(string workspaceId, IEnumerable<NewPlayer> newPlayers)
        {
            NewPlayers = newPlayers;
            WorkspaceId = workspaceId;
        }
    }
}
