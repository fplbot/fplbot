using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Endpoints;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplTransfersCommandHandler : IHandleMessages, IHandleEvent
    {
        private readonly IEnumerable<IPublisherBuilder> _publishers;
        private readonly IGameweekHelper _gameweekHelper;
        private readonly ITransfersByGameWeek _transfersClient;
        private readonly ITokenStore _tokenStore;
        private readonly IFetchFplbotSetup _setupFetcher;

        public FplTransfersCommandHandler(IEnumerable<IPublisherBuilder> publishers, IGameweekHelper gameweekHelper, ITransfersByGameWeek transfersByGameweek, ITokenStore tokenStore, IFetchFplbotSetup setupFetcher)
        {
            _publishers = publishers;
            _gameweekHelper = gameweekHelper;
            _transfersClient = transfersByGameweek;
            _tokenStore = tokenStore;
            _setupFetcher = setupFetcher;
        }

        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var gameweek = await _gameweekHelper.ExtractGameweekOrFallbackToCurrent(new MessageHelper(message.Bot), message.Text, "transfers {gw}");
            var token = await _tokenStore.GetTokenByTeamId(message.Team.Id);
            var setup = await _setupFetcher.GetSetupByToken(token);
            var messageToSend = await _transfersClient.GetTransfersByGameweekTexts(gameweek.Value, setup.LeagueId);
            
            foreach (var pBuilder in _publishers)
            {
                var p = await pBuilder.Build(message.Team.Id);
                await p.Publish(new Notification
                {
                    Recipient = message.ChatHub.Id,
                    Msg = messageToSend
                });
            }

            return new HandleResponse(messageToSend);
        }
        
        public async Task Handle(EventMetaData eventMetadata, SlackEvent slackEvent)
        {
            var rtmMessage = EventParser.ToBackCompatRtmMessage(eventMetadata, slackEvent);
            await Handle(rtmMessage);        
        }

        public bool ShouldHandle(SlackMessage message) => message.MentionsBot && message.Text.Contains("transfers");
        public bool ShouldHandle(SlackEvent slackEvent) => slackEvent is AppMentionEvent @event && @event.Text.Contains("transfers");

        public Tuple<string, string> GetHelpDescription() => new Tuple<string, string>("transfers {GW/''}", "Displays each team's transfers");
        public bool ShouldShowInHelp => true;
    }
}
