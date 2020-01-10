using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.SlackClients.Http;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplNextGameweekCommandHandler : IHandleMessages
    {
        private readonly IEnumerable<IPublisher> _publishers;
        private readonly ISlackClient _slackClient;
        private readonly IGameweekClient _gameweekClient;
        private readonly IFixtureClient _fixtureClient;
        private readonly ITeamsClient _teamsclient;

        public FplNextGameweekCommandHandler(IEnumerable<IPublisher> publishers, ISlackClient slackClient, IGameweekClient gameweekClient, IFixtureClient fixtureClient, ITeamsClient teamsclient)
        {
            _publishers = publishers;
            _slackClient = slackClient;
            _gameweekClient = gameweekClient;
            _fixtureClient = fixtureClient;
            _teamsclient = teamsclient;
        }

        public Tuple<string, string> GetHelpDescription()
        {
            return new Tuple<string, string>("next", "Henter neste gameweek");
        }

        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var users = await _slackClient.UsersList();
            var user = users.Members.SingleOrDefault(x => x.Id == message.User.Id);
            var userTzOffset = user?.Tz_Offset ?? 0;

            var gameweeks = await _gameweekClient.GetGameweeks();
            var nextGw = gameweeks.First(gw => gw.IsNext);
            var fixtures = await _fixtureClient.GetFixturesByGameweek(nextGw.Id);
            var teams = await _teamsclient.GetAllTeams();

            var textToSend = TextToSend(nextGw, fixtures, teams, userTzOffset);

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

        private static string TextToSend(Gameweek gameweek, ICollection<Fixture> fixtures, ICollection<Team> teams, int tzOffset)
        {
            var textToSend = $":information_source: <https://fantasy.premierleague.com/fixtures/{gameweek.Id}|{gameweek.Name.ToUpper()}>";
            textToSend += $"\nDeadline: {gameweek.Deadline.WithOffset(tzOffset):yyyy-MM-dd HH:mm}\n";
            
            var groupedByDay = fixtures.GroupBy(f => f.KickOffTime.Value.Date);

            foreach (var group in groupedByDay)
            {
                textToSend += $"\n{@group.Key.WithOffset(tzOffset):dddd}";
                foreach (var fixture in group)
                {
                    var homeTeam = teams.First(t => t.Id == fixture.HomeTeamId);
                    var awayTeam = teams.First(t => t.Id == fixture.AwayTeamId);
                    var fixtureKickOffTime = fixture.KickOffTime.Value.WithOffset(tzOffset);
                    textToSend += $"\n•{fixtureKickOffTime:HH:mm} {homeTeam.ShortName}-{awayTeam.ShortName}";
                }

                textToSend += "\n";
            }

            return textToSend;
        }
        
       

        public bool ShouldHandle(SlackMessage message)
        {
            return message.MentionsBot && message.Text.Contains("next");
        }

        public bool ShouldShowInHelp => true;
    }
}