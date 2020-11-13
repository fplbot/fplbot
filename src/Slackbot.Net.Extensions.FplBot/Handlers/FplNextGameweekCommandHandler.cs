using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.SlackClients.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    public class FplNextGameweekCommandHandler : HandleAppMentionBase
    {
        private readonly ISlackWorkSpacePublisher _workspacePublisher;
        private readonly ISlackClientBuilder _slackClientService;
        private readonly ITokenStore _tokenStore;
        private readonly IGameweekClient _gameweekClient;
        private readonly IFixtureClient _fixtureClient;
        private readonly ITeamsClient _teamsclient;

        public FplNextGameweekCommandHandler(ISlackWorkSpacePublisher workspacePublisher, IGameweekClient gameweekClient, IFixtureClient fixtureClient, ITeamsClient teamsclient, ISlackClientBuilder slackClientService, ITokenStore tokenStore)
        {
            _workspacePublisher = workspacePublisher;
            _gameweekClient = gameweekClient;
            _fixtureClient = fixtureClient;
            _teamsclient = teamsclient;
            _slackClientService = slackClientService;
            _tokenStore = tokenStore;
        }

        public override string Command => "next";

        public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent slackEvent)
        {
            var token = await _tokenStore.GetTokenByTeamId(eventMetadata.Team_Id);
            var slackClient = _slackClientService.Build(token);
            var usersTask = slackClient.UsersList();
            var gameweeksTask = _gameweekClient.GetGameweeks();
            var teamsTask = _teamsclient.GetAllTeams();

            var users = await usersTask;
            var gameweeks = await gameweeksTask;
            var teams = await teamsTask;

            var nextGw = gameweeks.First(gw => gw.IsNext);
            var fixtures = await _fixtureClient.GetFixturesByGameweek(nextGw.Id);

            var user = users.Members.FirstOrDefault(x => x.Id == slackEvent.User);
            var userTzOffset = user?.Tz_Offset ?? 0;

            var textToSend = TextToSend(nextGw, fixtures, teams, userTzOffset);

            await _workspacePublisher.PublishToWorkspace(eventMetadata.Team_Id, slackEvent.Channel, textToSend);
            
            return new EventHandledResponse(textToSend);
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

        public (string,string) GetHelpDescription() => (Command, "Displays the fixtures for next gameweek");
    }
}