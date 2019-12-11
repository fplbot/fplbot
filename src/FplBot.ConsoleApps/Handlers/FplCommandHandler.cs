using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fpl.Client;
using Slackbot.Net.Workers.Handlers;
using Slackbot.Net.Workers.Publishers;
using SlackConnector.Models;

namespace FplBot.ConsoleApps.Handlers
{
    public class FplCommandHandler : IHandleMessages
    {
        private readonly IEnumerable<IPublisher> _publishers;
        private readonly IFplClient _fplClient;

        public FplCommandHandler(IEnumerable<IPublisher> publishers, IFplClient fplClient)
        {
            _publishers = publishers;
            _fplClient = fplClient;
        }

        public Tuple<string, string> GetHelpDescription()
        {
            return new Tuple<string, string>("fpl", "Henter stillingen fra Blank-liga");
        }

        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var scoreboard = await _fplClient.GetScoreBoard("579157");
            var bootstrap = await _fplClient.GetBootstrap();
            var standings = Formatter.GetStandings(scoreboard, bootstrap);

            foreach (var p in _publishers)
            {
                await p.Publish(new Notification
                {
                    Recipient = message.ChatHub.Id,
                    Msg = standings
                });
            }

            return new HandleResponse(standings);
        }

        public bool ShouldHandle(SlackMessage message)
        {
            return message.MentionsBot && message.Text.Contains("fpl");
        }

        public bool ShouldShowInHelp => true;
    }
}