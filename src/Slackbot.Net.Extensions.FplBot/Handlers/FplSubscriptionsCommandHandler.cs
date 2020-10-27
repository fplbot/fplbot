using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplSubscriptionsCommandHandler : IHandleAppMentions
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

        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent appMentioned)
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

                var sb = new StringBuilder();

                sb.Append("This channel will receive notifications for: \n");

                sb.Append($"{string.Join(", ", team.Subscriptions)}");

                return sb.ToString();
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return $"Oops, could not get subscriptions.";
            }
        }

        public bool ShouldHandle(AppMentionEvent @event) => @event.Text.Contains("subscriptions");

        public (string, string) GetHelpDescription()
        {

            return ( "subscriptions", "List current subscriptions");
        }
    }
}