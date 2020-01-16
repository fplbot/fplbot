using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplCaptainCommandHandler : IHandleMessages
    {
        private readonly IEnumerable<IPublisher> _publishers;
        private readonly ICaptainsByGameWeek _captainsByGameWeek;
        private readonly IGameweekHelper _gameweekHelper;

        public FplCaptainCommandHandler(IEnumerable<IPublisher> publishers, IGameweekClient gameweekClient, ICaptainsByGameWeek captainsByGameWeek,  IGameweekHelper gameweekHelper)
        {
            _publishers = publishers;
            _captainsByGameWeek = captainsByGameWeek;
            _gameweekHelper = gameweekHelper;
        }

        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var gameWeek = await _gameweekHelper.ExtractGameweekOrFallbackToCurrent(message.Text, "captains {gw}");

            var messageToSend = gameWeek.HasValue ? await _captainsByGameWeek.GetCaptainsByGameWeek(gameWeek.Value) : "Invalid gameweek :grimacing:";

            foreach (var p in _publishers)
            {
                await p.Publish(new Notification
                {
                    Recipient = message.ChatHub.Id,
                    Msg = messageToSend
                });
            }

            return new HandleResponse(messageToSend);
        }

        public bool ShouldHandle(SlackMessage message) => message.MentionsBot && message.Text.Contains("captains");
        public Tuple<string, string> GetHelpDescription() => new Tuple<string, string>("captains {GW/''}", "Display captain picks in the league");
        public bool ShouldShowInHelp => true;
    }
}
