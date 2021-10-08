using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Slack.Abstractions;
using FplBot.Slack.Data.Abstractions;
using FplBot.Slack.Data.Models;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.Slack.Handlers.FplEvents
{
    public class NearDeadlineHandler :
        IHandleMessages<OneHourToDeadline>,
        IHandleMessages<TwentyFourHoursToDeadline>,
        IHandleMessages<PublishDeadlineNotificationToSlackWorkspace>
    {
        private readonly ISlackWorkSpacePublisher _workspacePublisher;
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ISlackClientBuilder _builder;
        private readonly ILogger<NearDeadlineHandler> _logger;
        private readonly IGlobalSettingsClient _globalSettingsClient;
        private readonly IFixtureClient _fixtures;

        public NearDeadlineHandler(ISlackWorkSpacePublisher workspacePublisher, ISlackTeamRepository teamRepo,
            ISlackClientBuilder builder, IGlobalSettingsClient globalSettingsClient, IFixtureClient fixtures,
            ILogger<NearDeadlineHandler> logger)
        {
            _workspacePublisher = workspacePublisher;
            _teamRepo = teamRepo;
            _builder = builder;
            _logger = logger;
            _globalSettingsClient = globalSettingsClient;
            _fixtures = fixtures;
        }

        public async Task Handle(OneHourToDeadline message, IMessageHandlerContext context)
        {
            _logger.LogInformation($"Notifying about 60 minutes to (gw{message.GameweekNearingDeadline.Id}) deadline");
            var allSlackTeams = await _teamRepo.GetAllTeams();
            foreach (var team in allSlackTeams)
            {
                if (team.HasRegisteredFor(EventSubscription.Deadlines))
                {
                    var text = $"<!channel> ⏳ Gameweek {message.GameweekNearingDeadline.Id} deadline in 60 minutes!";
                    var command = new PublishToSlack(team.TeamId, team.FplBotSlackChannel, text);
                    await context.SendLocal(command);
                }
            }
        }

        public async Task Handle(TwentyFourHoursToDeadline message, IMessageHandlerContext context)
        {
            _logger.LogInformation($"Notifying about 24h to (gw{message.GameweekNearingDeadline.Id}) deadline");

            var allSlackTeams = await _teamRepo.GetAllTeams();
            foreach (var team in allSlackTeams)
            {
                if (team.HasRegisteredFor(EventSubscription.Deadlines))
                {
                    var command = new PublishDeadlineNotificationToSlackWorkspace(team.TeamId, message.GameweekNearingDeadline);
                    await context.SendLocal(command);
                }
            }
        }

        public async Task Handle(PublishDeadlineNotificationToSlackWorkspace message, IMessageHandlerContext context)
        {
            string notification = $"⏳ Gameweek {message.Gameweek.Id} deadline in 24 hours!";

            var team = await _teamRepo.GetTeam(message.WorkspaceId);
            await PublishToTeam();

            async Task PublishToTeam()
            {
                var slackClient = _builder.Build(team.AccessToken);
                var res = await slackClient.ChatPostMessage(team.FplBotSlackChannel, notification);
                if (res.Ok)
                {
                    await PublishFixtures(slackClient, res.ts);
                }
            }

            async Task PublishFixtures(ISlackClient slackClient, string ts)
            {
                var fixtures = await _fixtures.GetFixturesByGameweek(message.Gameweek.Id);
                var teams = (await _globalSettingsClient.GetGlobalSettings()).Teams;
                var users = await slackClient.UsersList();
                var user = users.Members.FirstOrDefault(u =>
                    u.Is_Admin); // could have selected app_install user here, if we had this stored
                var userTzOffset = user?.Tz_Offset ?? 0;
                var messageGameweekNearingDeadline = message.Gameweek;
                var fixturesList = Formatter.FixturesForGameweek(messageGameweekNearingDeadline.Id,
                    messageGameweekNearingDeadline.Name, messageGameweekNearingDeadline.Deadline, fixtures, teams,
                    tzOffset: userTzOffset);

                    await slackClient.ChatPostMessage(new ChatPostMessageRequest
                    {
                        Channel = team.FplBotSlackChannel,
                        thread_ts = ts,
                        Text = fixturesList,
                        unfurl_links = "false"
                    });
            }
        }
    }
}
