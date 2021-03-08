using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.Data;
using Fpl.Data.Abstractions;
using Fpl.Data.Models;
using Fpl.Data.Repositories;
using FplBot.Core.Abstractions;
using FplBot.Core.Extensions;
using FplBot.Core.Helpers;
using FplBot.Core.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.Core.GameweekLifecycle.Handlers
{
    public class NearDeadlineHandler : INotificationHandler<OneHourToDeadline>, 
                                       INotificationHandler<TwentyFourHoursToDeadline>
    {
        private readonly ISlackWorkSpacePublisher _workspacePublisher;
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ISlackClientBuilder _builder;
        private readonly ILogger<NearDeadlineHandler> _logger;
        private readonly IGlobalSettingsClient _globalSettingsClient;
        private readonly IFixtureClient _fixtures;

        public NearDeadlineHandler(ISlackWorkSpacePublisher workspacePublisher, ISlackTeamRepository teamRepo, ISlackClientBuilder builder, IGlobalSettingsClient globalSettingsClient, IFixtureClient fixtures, ILogger<NearDeadlineHandler> logger)
        {
            _workspacePublisher = workspacePublisher;
            _teamRepo = teamRepo;
            _builder = builder;
            _logger = logger;
            _globalSettingsClient = globalSettingsClient;
            _fixtures = fixtures;
        }
        
        public async Task Handle(OneHourToDeadline notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Notifying about 60 minutes to (gw{notification.Gameweek.Id}) deadline");
            var allSlackTeams = await _teamRepo.GetAllTeams();
            foreach (var team in allSlackTeams)
            {
                if (team.Subscriptions.ContainsSubscriptionFor(EventSubscription.Deadlines))
                {
                    await _workspacePublisher.PublishToWorkspace(team.TeamId, team.FplBotSlackChannel,$"<!channel> ⏳ Gameweek {notification.Gameweek.Id} deadline in 60 minutes!");
                }
            }
        }

        public async Task Handle(TwentyFourHoursToDeadline notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Notifying about 24h to (gw{notification.Gameweek.Id}) deadline");
            var fixtures = await _fixtures.GetFixturesByGameweek(notification.Gameweek.Id);
            var teams = (await _globalSettingsClient.GetGlobalSettings()).Teams;
            
            var allSlackTeams = await _teamRepo.GetAllTeams();
            string message = $"⏳ Gameweek {notification.Gameweek.Id} deadline in 24 hours!";
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
                var fixturesList = Formatter.FixturesForGameweek(notification.Gameweek, fixtures, teams, tzOffset: userTzOffset);
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
    }
}