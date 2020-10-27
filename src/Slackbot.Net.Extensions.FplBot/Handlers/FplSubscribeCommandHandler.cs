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
    internal class FplSubscribeCommandHandler : IHandleAppMentions
    {
        private readonly ISlackWorkSpacePublisher _workspacePublisher;
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ILogger<FplSubscriptionsCommandHandler> _logger;

        public FplSubscribeCommandHandler(ISlackWorkSpacePublisher workspacePublisher, ISlackTeamRepository teamRepo, ILogger<FplSubscriptionsCommandHandler> logger)
        {
            _workspacePublisher = workspacePublisher;
            _teamRepo = teamRepo;
            _logger = logger;
        }

        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent appMentioned)
        {
            var subscriptionInfo = await GetAndUpdateSubscriptionInfo(eventMetadata.Team_Id, appMentioned);
            await _workspacePublisher.PublishToWorkspace(eventMetadata.Team_Id, appMentioned.Channel, subscriptionInfo);
            return new EventHandledResponse(subscriptionInfo);
        }

        private async Task<string> GetAndUpdateSubscriptionInfo(string teamId, AppMentionEvent appMentioned)
        {
            try
            {
                var team = await _teamRepo.GetTeam(teamId);
                var currentSubscriptions = team.Subscriptions;

                var inputSubscriptions = ParseSubscriptionsFromInput(appMentioned);

                IEnumerable<EventSubscription> newSubscriptions;
                if (appMentioned.Text.Contains("unsubscribe")) { newSubscriptions = currentSubscriptions.Except(inputSubscriptions); }
                else { newSubscriptions = currentSubscriptions.Union(inputSubscriptions); }

                await _teamRepo.UpdateSubscriptions(teamId, newSubscriptions);

                return FormatSubscriptionMessage(newSubscriptions);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message,e);
                return $"Oops, could not update subscriptions.";
            }
        }

        private IEnumerable<EventSubscription> ParseSubscriptionsFromInput(AppMentionEvent appMentioned)
        {

            var stringListOfEvents = MessageHelper.ExtractArgs(appMentioned.Text, "subscribe {args}");
            return (IEnumerable<EventSubscription>)stringListOfEvents.Split(",")
                .Select(e =>
                {
                    var isValid = Enum.TryParse(e.Trim(), out EventSubscription eventSubscription);
                    return isValid ? eventSubscription : (EventSubscription?)null;
                }).Where(e => e.HasValue && e != null).ToList();
        }

        private string FormatSubscriptionMessage(IEnumerable<EventSubscription> eventSubscriptions)
        {
            var sb = new StringBuilder();

            sb.Append("Updated subscriptions :sparkles:\n");

            sb.Append($"You will now receive updates for: {string.Join(",", eventSubscriptions)}");

            return sb.ToString();
        }

        public bool ShouldHandle(AppMentionEvent @event) => @event.Text.Contains("subscribe");

        public (string, string) GetHelpDescription()
        {
            var sb = new StringBuilder();

            sb.Append("Update what notifications fplbot should post. (");

            sb.Append($"{string.Join(", ", Enum.GetNames(typeof(EventSubscription)))})");

            return (
            "subscribe {comma separated list of events}",
            sb.ToString());
        }
    }
}