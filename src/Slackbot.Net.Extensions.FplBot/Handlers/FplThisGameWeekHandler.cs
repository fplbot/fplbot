using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot.RecurringActions;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplThisGameWeekHandler : IHandleEvent
    {
        private readonly IHandleGameweekStarted _notifier;
        private readonly IGameweekClient _gwClient;

        public FplThisGameWeekHandler(IHandleGameweekStarted notifier, IGameweekClient gwClient)
        {
            _notifier = notifier;
            _gwClient = gwClient;
        }
        
        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var gameweeks = await _gwClient.GetGameweeks();
            var current = gameweeks.First(gw => gw.IsCurrent);
            await _notifier.HandleGameweekStarted(current.Id);
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