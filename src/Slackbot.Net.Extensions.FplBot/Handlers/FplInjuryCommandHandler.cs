using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle;
using Slackbot.Net.Extensions.FplBot.Helpers;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplInjuryCommandHandler : IHandleEvent
    {
        private readonly ISlackWorkSpacePublisher _workspacePublisher;
        private readonly IPlayerClient _playerClient;

        public FplInjuryCommandHandler(ISlackWorkSpacePublisher workspacePublisher, IPlayerClient playerClient)
        {
            _workspacePublisher = workspacePublisher;
            _playerClient = playerClient;
        }
        
        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, SlackEvent slackEvent)
        {
            var message = slackEvent as AppMentionEvent;
            var allPlayers = await _playerClient.GetAllPlayers();

            var injuredPlayers = FindInjuredPlayers(allPlayers);

            var textToSend = Formatter.GetInjuredPlayers(injuredPlayers);

            if (string.IsNullOrEmpty(textToSend))
            {
                return new EventHandledResponse("Not found");
            }
            await _workspacePublisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, textToSend);

            return new EventHandledResponse(textToSend);
        }

        private static IEnumerable<Player> FindInjuredPlayers(IEnumerable<Player> players)
        {
            return players.Where(p => p.OwnershipPercentage > 5 && IsInjured(p));
        }

        private static bool IsInjured(Player player)
        {
            return player.ChanceOfPlayingNextRound != "100" && player.ChanceOfPlayingNextRound != null;
        }

        public bool ShouldHandle(SlackEvent slackEvent) => slackEvent is AppMentionEvent @event && @event.Text.Contains("injuries");

        public (string,string) GetHelpDescription() => ("injuries", "See injured players owned by more than 5 %");
    }

}
