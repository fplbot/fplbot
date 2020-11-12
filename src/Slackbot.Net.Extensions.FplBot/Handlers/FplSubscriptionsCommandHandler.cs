using Microsoft.Extensions.Logging;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplSubscriptionsCommandHandler : HandleAppMentionBase
    {
        private readonly ISlackWorkSpacePublisher _workspacePublisher;
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ILogger<FplSubscriptionsCommandHandler> _logger;

        public FplSubscriptionsCommandHandler(ISlackWorkSpacePublisher workspacePublisher, ISlackTeamRepository teamRepo, ILogger<FplSubscriptionsCommandHandler> logger)
        {
            _workspacePublisher = workspacePublisher;
            _teamRepo = teamRepo;
            _logger = logger;
        }

        public override string Command => "subscriptions";

        public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent appMentioned)
        {
            var subscriptionInfo = await GetCurrentSubscriptions(eventMetadata.Team_Id, appMentioned);
            await _workspacePublisher.PublishToWorkspace(eventMetadata.Team_Id, appMentioned.Channel, subscriptionInfo);
            return new EventHandledResponse(subscriptionInfo);
        }

        private async Task<string> GetCurrentSubscriptions(string teamId, AppMentionEvent appMentioned)
        {
            try
            {
                var team = await _teamRepo.GetTeam(teamId);
                var currentSubscriptions = team.Subscriptions;

                if (currentSubscriptions.Count() < 1)
                {
                    return "You are not subscribing to any fplbot updates :disappointed:";
                }

                var sb = new StringBuilder();

                sb.Append("This channel will receive notifications for: \n");

                sb.Append($"{Formatter.BulletPoints(team.Subscriptions)}");

                return sb.ToString();
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return $"Oops, could not get subscriptions.";
            }
        }

        public (string, string) GetHelpDescription()
        {
            return (Command, "List current subscriptions");
        }
    }
}