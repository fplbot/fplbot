using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.RecurringActions;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplThisGameWeekHandler : IHandleEvent
    {
        private readonly IHandleGameweekStarted _notifier;
        private readonly IEnumerable<IPublisherBuilder> _publishers;
        private readonly IGameweekClient _gwClient;
        private readonly ICaptainsByGameWeek _captainsByGameweek;
        private readonly ITransfersByGameWeek _transfersByGameweek;
        private readonly IFetchFplbotSetup _setupFetcher;
        private ITokenStore _tokenStore;

        public FplThisGameWeekHandler(IEnumerable<IPublisherBuilder> publishers, IGameweekClient gwClient, ICaptainsByGameWeek captainsByGameweek, ITransfersByGameWeek transfersByGameWeek, IFetchFplbotSetup setupFetcher, ITokenStore tokenStore)
        {
            _publishers = publishers;
            _gwClient = gwClient;
            _captainsByGameweek = captainsByGameweek;
            _transfersByGameweek = transfersByGameWeek;
            _setupFetcher = setupFetcher;
            _tokenStore = tokenStore;
        }
        
        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var gameweeks = await _gwClient.GetGameweeks();
            var newGameweek = gameweeks.First(gw => gw.IsCurrent);
            var token = await _tokenStore.GetTokenByTeamId(message.Team.Id);
            var setup = await _setupFetcher.GetSetupByToken(token);            
            
            var captains = await _captainsByGameweek.GetCaptainsByGameWeek(newGameweek.Id, setup.LeagueId);
            var captainsChart = await _captainsByGameweek.GetCaptainsChartByGameWeek(newGameweek.Id, setup.LeagueId);
            var transfers = await _transfersByGameweek.GetTransfersByGameweekTexts(newGameweek.Id, setup.LeagueId);
            var msgs = new[] {captains, captainsChart, transfers};
            foreach (var pBuilder in _publishers)
            {
                var p = await pBuilder.Build(message.Team.Id);
                foreach (var msg in msgs)
                {
                    await p.Publish(new Notification
                    {
                        Recipient = message.ChatHub.Id,
                        Msg = msg
                    }); 
                }
                
            }
            return new HandleResponse("OK");
        }

        public bool ShouldHandle(SlackEvent slackEvent) => slackEvent is AppMentionEvent @event && @event.Text.Contains("currentgw");
        
        public (string,string) GetHelpDescription() => ("currentgw", "Publish all details about current gw (captains,transfers");
        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, SlackEvent slackEvent)
        {
            var rtmMessage = EventParser.ToBackCompatRtmMessage(eventMetadata, slackEvent);
            var messageHandled = await Handle(rtmMessage);     
            return new EventHandledResponse(messageHandled.HandledMessage);
        }
        
    }
}