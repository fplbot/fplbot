using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplTransfersCommandHandler : IHandleMessages
    {
        private readonly IEnumerable<IPublisherBuilder> _publishers;
        private readonly IGameweekHelper _gameweekHelper;
        private readonly ITransfersByGameWeek _transfersClient;

        public FplTransfersCommandHandler(IEnumerable<IPublisherBuilder> publishers, IGameweekHelper gameweekHelper, ITransfersByGameWeek transfersByGameweek)
        {
            _publishers = publishers;
            _gameweekHelper = gameweekHelper;
            _transfersClient = transfersByGameweek;
        }

        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var gameweek = await _gameweekHelper.ExtractGameweekOrFallbackToCurrent(new MessageHelper(message.Bot), message.Text, "transfers {gw}");
            var messageToSend = await _transfersClient.GetTransfersByGameweekTexts(gameweek);
            
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

        public bool ShouldHandle(SlackMessage message) => message.MentionsBot && message.Text.Contains("transfers");
        public Tuple<string, string> GetHelpDescription() => new Tuple<string, string>("transfers {GW/''}", "Displays each team's transfers");
        public bool ShouldShowInHelp => true;
    }
}
