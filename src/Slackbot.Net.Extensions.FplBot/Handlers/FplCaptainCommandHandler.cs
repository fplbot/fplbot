using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Abstractions.Publishers;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    public class FplCaptainCommandHandler : IHandleMessages
    {
        private readonly IEnumerable<IPublisher> _publishers;
        private readonly IGameweekClient _gameweekClient;
        private readonly ICaptainsByGameWeek _captainsByGameWeek;
        private readonly BotDetails _botDetails;

        public FplCaptainCommandHandler(IEnumerable<IPublisher> publishers, IGameweekClient gameweekClient, ICaptainsByGameWeek captainsByGameWeek,  BotDetails botDetails)
        {
            _publishers = publishers;
            _gameweekClient = gameweekClient;
            _captainsByGameWeek = captainsByGameWeek;
            _botDetails = botDetails;
        }

        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var gameWeek = await GetGameweek(message);

            var messageToSend = gameWeek.HasValue ? await _captainsByGameWeek.GetCaptainsByGameWeek(gameWeek.Value) : "Ugyldig gameweek :grimacing:";

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

        private async Task<int?> GetGameweek(SlackMessage message)
        {
            var replacements = new[]
            {
                new {Find = "@fplbot", Replace = ""},
                new {Find = $"<@{_botDetails.Id}>", Replace = ""}, // @fplbot-userid
                new {Find = "captains", Replace = ""}
            };

            var name = message.Text;

            foreach (var set in replacements)
            {
                name = name.Replace(set.Find, set.Replace).Trim();
            }

            if (name != "")
            {
                try
                {
                    return int.Parse(name);
                }
                catch (FormatException)
                {
                    return null;
                }
            }

            var gameweeks = await _gameweekClient.GetGameweeks();
            var currentGw = gameweeks.SingleOrDefault(x => x.IsCurrent)?.Id;

            return currentGw;
        }

 

        public Tuple<string, string> GetHelpDescription()
        {
            return new Tuple<string, string>("captains {GW/''}", "Henter kapteinsvalg for liga");
        }

        public bool ShouldHandle(SlackMessage message)
        {
            return message.MentionsBot && message.Text.Contains("captains");
        }

        public bool ShouldShowInHelp => true;
    }
}
