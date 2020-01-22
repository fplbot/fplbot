using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.SlackClients.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Slackbot.Net.Dynamic;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplNextGameweekCommandHandler : IHandleMessages
    {
        private readonly IEnumerable<IPublisherBuilder> _publishers;
        private readonly ISlackClientService _slackClientBuilder;
        private readonly IGameweekClient _gameweekClient;
        private readonly IFixtureClient _fixtureClient;
        private readonly ITeamsClient _teamsclient;

        public FplNextGameweekCommandHandler(IEnumerable<IPublisherBuilder> publishers, ISlackClientService slackClientBuilder, IGameweekClient gameweekClient, IFixtureClient fixtureClient, ITeamsClient teamsclient)
        {
            _publishers = publishers;
            _slackClientBuilder = slackClientBuilder;
            _gameweekClient = gameweekClient;
            _fixtureClient = fixtureClient;
            _teamsclient = teamsclient;
        }

        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var slackClient = await _slackClientBuilder.CreateClient(message.Team.Id);
            var usersTask = slackClient.UsersList();
            var gameweeksTask = _gameweekClient.GetGameweeks();
            var teamsTask = _teamsclient.GetAllTeams();
            
            var users = await usersTask;
            var gameweeks = await gameweeksTask;
            var teams = await teamsTask;

            var nextGw = gameweeks.First(gw => gw.IsNext);
            var fixtures = await _fixtureClient.GetFixturesByGameweek(nextGw.Id);

            var user = users.Members.FirstOrDefault(x => x.Id == message.User?.Id);
            var userTzOffset = user?.Tz_Offset ?? 0;

            var textToSend = TextToSend(nextGw, fixtures, teams, userTzOffset);

            foreach (var pBuilder in _publishers)
            {
                var p = await pBuilder.Build(message.Team.Id);
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
                textToSend += $"\n{group.Key.WithOffset(tzOffset):dddd}";
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

        public bool ShouldHandle(SlackMessage message) => message.MentionsBot && message.Text.Contains("next");
        public Tuple<string, string> GetHelpDescription() => new Tuple<string, string>("next", "Displays the fixtures for next gameweek");
        public bool ShouldShowInHelp => true;
    }
}