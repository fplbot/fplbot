using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Slackbot.Net.Workers.Handlers;
using Slackbot.Net.Workers.Publishers;
using SlackConnector.Models;

namespace FplBot.ConsoleApps.Handlers
{
    public class FplNextGameweekCommandHandler : IHandleMessages
    {
        private readonly IEnumerable<IPublisher> _publishers;
        private readonly IGameweekClient _gameweekClient;
        private readonly IFixtureClient _fixtureClient;
        private readonly ITeamsClient _teamsclient;

        public FplNextGameweekCommandHandler(IEnumerable<IPublisher> publishers, IGameweekClient gameweekClient, IFixtureClient fixtureClient, ITeamsClient teamsclient)
        {
            _publishers = publishers;
            _gameweekClient = gameweekClient;
            _fixtureClient = fixtureClient;
            _teamsclient = teamsclient;
        }

        public Tuple<string, string> GetHelpDescription()
        {
            return new Tuple<string, string>("nextgw", "Henter neste gameweek");
        }

        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var gameweeks = await _gameweekClient.GetGameweeks();
            var currentGw = gameweeks.SingleOrDefault(x => x.IsCurrent)?.Id;
            var fixtures = await _fixtureClient.GetFixturesByGameweek(currentGw.Value+1);
            var teams = await _teamsclient.GetAllTeams();

            var textToSend = "";
            foreach (var fixture in fixtures)
            {
                var homeTeam = teams.First(t => t.Id == fixture.HomeTeamId);
                var awayTeam = teams.First(t => t.Id == fixture.AwayTeamId);
                textToSend += $"\n * {homeTeam.ShortName}-{awayTeam.ShortName}";
            }
            
            foreach (var p in _publishers)
            {
                await p.Publish(new Notification
                {
                    Recipient = message.ChatHub.Id,
                    Msg = textToSend
                });
            }

            return new HandleResponse(textToSend);
        }

        public bool ShouldHandle(SlackMessage message)
        {
            return message.MentionsBot && message.Text.Contains("nextgw");
        }

        public bool ShouldShowInHelp => true;
    }
}