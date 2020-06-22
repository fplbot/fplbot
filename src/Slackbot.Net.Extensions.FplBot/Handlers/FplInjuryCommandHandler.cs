using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot.Helpers;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplInjuryCommandHandler : IHandleEvent
    {
        private readonly IEnumerable<IPublisherBuilder> _publishers;
        private readonly IPlayerClient _playerClient;

        public FplInjuryCommandHandler(IEnumerable<IPublisherBuilder> publishers, IPlayerClient playerClient)
        {
            _publishers = publishers;
            _playerClient = playerClient;
        }
        public async Task<HandleResponse> Handle(SlackMessage message)
        {

            var allPlayers = await _playerClient.GetAllPlayers();

            var injuredPlayers = FindInjuredPlayers(allPlayers);

            var textToSend = Formatter.GetInjuredPlayers(injuredPlayers);

            foreach (var pBuilder in _publishers)
            {
                var p = await pBuilder.Build(message.Team.Id);
                await p.Publish(new Notification
                {
                    Recipient = message.ChatHub.Id,
                    Msg = textToSend
                });
            }

            if (string.IsNullOrEmpty(textToSend))
            {
                return new HandleResponse("Not found");

            }
            return new HandleResponse(textToSend);
        }
        
        public async Task Handle(EventMetaData eventMetadata, SlackEvent slackEvent)
        {
            var rtmMessage = EventParser.ToBackCompatRtmMessage(eventMetadata, slackEvent);
            await Handle(rtmMessage);
        }

        private static IEnumerable<Player> FindInjuredPlayers(IEnumerable<Player> players)
        {
            return players.Where(p => p.OwnershipPercentage > 5 && IsInjured(p));
        }

        private static bool IsInjured(Player player)
        {
            return player.ChanceOfPlayingNextRound != "100" && player.ChanceOfPlayingNextRound != null;
        }

        public bool ShouldHandle(SlackMessage message) => message.MentionsBot && message.Text.Contains("injuries");
        public bool ShouldHandle(SlackEvent slackEvent) => slackEvent is AppMentionEvent @event && @event.Text.Contains("injuries");

        public (string,string) GetHelpDescription() => ("injuries", "See injured players owned by more than 5 %");
        public bool ShouldShowInHelp => true;
    }

}
