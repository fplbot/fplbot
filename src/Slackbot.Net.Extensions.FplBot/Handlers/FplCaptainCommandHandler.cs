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
    internal class FplCaptainCommandHandler : IHandleMessages, IHandleEvent
    {
        private readonly IEnumerable<IPublisherBuilder> _publishers;
        private readonly ICaptainsByGameWeek _captainsByGameWeek;
        private readonly IGameweekHelper _gameweekHelper;
        private readonly ITokenStore _tokenStore;
        private readonly IFetchFplbotSetup _setupFetcher;

        public FplCaptainCommandHandler(
            IEnumerable<IPublisherBuilder> publishers, 
            ICaptainsByGameWeek captainsByGameWeek,  
            IGameweekHelper gameweekHelper,
            ITokenStore tokenStore,
            IFetchFplbotSetup setupFetcher)
        {
            _publishers = publishers;
            _captainsByGameWeek = captainsByGameWeek;
            _gameweekHelper = gameweekHelper;
            _tokenStore = tokenStore;
            _setupFetcher = setupFetcher;
        }

        public async Task<HandleResponse> Handle(SlackMessage incomingMessage)
        {
            var isChartRequest = incomingMessage.Text.Contains("chart");

            var gwPattern = "captains {gw}";
            if (isChartRequest)
            {
                gwPattern = "captains chart {gw}|captains {gw} chart";
            }
            var gameWeek = await _gameweekHelper.ExtractGameweekOrFallbackToCurrent(new MessageHelper(incomingMessage.Bot), incomingMessage.Text, gwPattern);

            if (!gameWeek.HasValue)
            {
                return await Publish(incomingMessage, "Invalid gameweek :grimacing:");
            }

            var token = await _tokenStore.GetTokenByTeamId(incomingMessage.Team.Id);
            var setup = await _setupFetcher.GetSetupByToken(token);
            var outgoingMessage = isChartRequest ? 
                await _captainsByGameWeek.GetCaptainsChartByGameWeek(gameWeek.Value, setup.LeagueId) : 
                await _captainsByGameWeek.GetCaptainsByGameWeek(gameWeek.Value, setup.LeagueId);

            return await Publish(incomingMessage, outgoingMessage);
        }
        
        public async Task Handle(EventMetaData eventMetadata, SlackEvent slackEvent)
        {
            var rtmMessage = EventParser.ToBackCompatRtmMessage(eventMetadata, slackEvent);
            await Handle(rtmMessage);
        }

        private async Task<HandleResponse> Publish(SlackMessage incomingMessage, string outgoingMessage)
        {
            foreach (var pBuilder in _publishers)
            {
                var p = await pBuilder.Build(incomingMessage.Team.Id);
                await p.Publish(new Notification
                {
                    Recipient = incomingMessage.ChatHub.Id,
                    Msg = outgoingMessage
                });
            }

            return new HandleResponse(outgoingMessage);
        }

        public bool ShouldHandle(SlackMessage message) => message.MentionsBot && message.Text.Contains("captains");
        public bool ShouldHandle(SlackEvent slackEvent) => slackEvent is AppMentionEvent @event && @event.Text.Contains("captains");

        public Tuple<string, string> GetHelpDescription() => new Tuple<string, string>("captains [chart] {GW/''}", "Display captain picks in the league. Add \"chart\" to visualize it in a chart.");
        public bool ShouldShowInHelp => true;
    }
}
