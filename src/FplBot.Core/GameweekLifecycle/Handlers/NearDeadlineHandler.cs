using System;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Core.Abstractions;
using FplBot.Core.Extensions;
using FplBot.Core.Helpers;
using Microsoft.Extensions.Logging;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.Core.GameweekLifecycle.Handlers
{
    public class NearDeadlineHandler
    {
        private readonly ISlackWorkSpacePublisher _workspacePublisher;
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ISlackClientBuilder _builder;
        private readonly ILogger<NearDeadlineHandler> _logger;
        private readonly ITeamsClient _teams;
        private readonly IFixtureClient _fixtures;

        public NearDeadlineHandler(ISlackWorkSpacePublisher workspacePublisher, ISlackTeamRepository teamRepo, ISlackClientBuilder builder, ITeamsClient teams, IFixtureClient fixtures, ILogger<NearDeadlineHandler> logger)
        {
            _workspacePublisher = workspacePublisher;
            _teamRepo = teamRepo;
            _builder = builder;
            _logger = logger;
            _teams = teams;
            _fixtures = fixtures;
        }

        public async Task HandleTwentyFourHoursToDeadline(Gameweek gameweek)
        {
            _logger.LogInformation($"Notifying about 24h to (gw{gameweek.Id}) deadline");
            var fixtures = await _fixtures.GetFixturesByGameweek(gameweek.Id);
            var teams = await _teams.GetAllTeams();
            
            var allSlackTeams = await _teamRepo.GetAllTeams();
            string message = $"⏳ Gameweek {gameweek.Id} deadline in 24 hours!";
            foreach (var team in allSlackTeams)
            {
                if (team.Subscriptions.ContainsSubscriptionFor(EventSubscription.Deadlines))
                {
                    await PublishToTeam(team);    
                }
            }

            async Task PublishToTeam(SlackTeam team)
            {
                var slackClient = _builder.Build(team.AccessToken);
                try
                {
                    var res = await slackClient.ChatPostMessage(team.FplBotSlackChannel, message);
                    if (res.Ok)
                    {
                        await PublishFixtures(slackClient, res.ts, team);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, e.Message);
                }
            }

            async Task PublishFixtures(ISlackClient slackClient, string ts, SlackTeam team)
            {
                var users = await slackClient.UsersList();
                var user = users.Members.FirstOrDefault(u => u.Is_Admin); // could have selected app_install user here, if we had this stored
                var userTzOffset = user?.Tz_Offset ?? 0;
                var fixturesList = Formatter.FixturesForGameweek(gameweek, fixtures, teams, tzOffset: userTzOffset);
                try
                {
                    await slackClient.ChatPostMessage(new ChatPostMessageRequest
                    {
                        Channel = team.FplBotSlackChannel, thread_ts = ts, Text = fixturesList, unfurl_links = "false"
                    });
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, e.Message);
                }
            }
        }
        
        public async Task HandleOneHourToDeadline(Gameweek gameweek)
        {
            _logger.LogInformation($"Notifying about 60 minutes to (gw{gameweek.Id}) deadline");
            var allSlackTeams = await _teamRepo.GetAllTeams();
            foreach (var team in allSlackTeams)
            {
                if (team.Subscriptions.ContainsSubscriptionFor(EventSubscription.Deadlines))
                {
                    await _workspacePublisher.PublishToWorkspace(team.TeamId, team.FplBotSlackChannel,$"<!channel> ⏳ Gameweek {gameweek.Id} deadline in 60 minutes!");
                }
            }
        }
    }
}