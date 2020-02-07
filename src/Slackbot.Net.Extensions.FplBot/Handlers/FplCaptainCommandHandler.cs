using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplCaptainCommandHandler : IHandleMessages
    {
        private readonly IEnumerable<IPublisherBuilder> _publishers;
        private readonly ICaptainsByGameWeek _captainsByGameWeek;
        private readonly IGameweekHelper _gameweekHelper;

        public FplCaptainCommandHandler(
            IEnumerable<IPublisherBuilder> publishers, 
            ICaptainsByGameWeek captainsByGameWeek,  
            IGameweekHelper gameweekHelper)
        {
            _publishers = publishers;
            _captainsByGameWeek = captainsByGameWeek;
            _gameweekHelper = gameweekHelper;
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
            
            var outgoingMessage = isChartRequest ? 
                await _captainsByGameWeek.GetCaptainsChartByGameWeek(gameWeek.Value) : 
                await _captainsByGameWeek.GetCaptainsByGameWeek(gameWeek.Value);

            return await Publish(incomingMessage, outgoingMessage);
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
        public Tuple<string, string> GetHelpDescription() => new Tuple<string, string>("captains [chart] {GW/''}", "Display captain picks in the league. Add \"chart\" too visualize it in a chart.");
        public bool ShouldShowInHelp => true;
    }
}
