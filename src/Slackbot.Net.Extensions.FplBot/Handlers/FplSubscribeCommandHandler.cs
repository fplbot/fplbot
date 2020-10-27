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
using Slackbot.Net.Extensions.FplBot.Extensions;
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


                IEnumerable<EventSubscription> newSubscriptions = GetNewSubscriptionList(
                    appMentioned.Text.Contains("unsubscribe"),
                    ParseSubscriptionsFromInput(appMentioned),
                    team.Subscriptions
                );

                await _teamRepo.UpdateSubscriptions(teamId, newSubscriptions);

                return FormatSubscriptionMessage(newSubscriptions);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message,e);
                return $"Oops, could not update subscriptions.";
            }
        }

        private IEnumerable<EventSubscription> GetNewSubscriptionList(
            bool isUnsubscribe,
            IEnumerable<EventSubscription> inputSubscriptions,
            IEnumerable<EventSubscription> currentSubscriptions
        )
        {
            var includesAll = inputSubscriptions.Contains(EventSubscription.All);

            if (includesAll && isUnsubscribe) { return new List<EventSubscription>(); }
            if (includesAll && !isUnsubscribe) { return new List<EventSubscription>() { EventSubscription.All }; }

            if (isUnsubscribe) {return currentSubscriptions.Except(inputSubscriptions);}

            return currentSubscriptions.Union(inputSubscriptions);
        }

        private IEnumerable<EventSubscription> ParseSubscriptionsFromInput(AppMentionEvent appMentioned)
        {
            var stringListOfEvents = MessageHelper.ExtractArgs(appMentioned.Text, "subscribe {args}");
            return stringListOfEvents.ParseSubscriptionString(delimiter: ",");
        }

        private string FormatSubscriptionMessage(IEnumerable<EventSubscription> eventSubscriptions)
        {
            var sb = new StringBuilder();

            sb.Append("Updated subscriptions :sparkles:\n");

            sb.Append($"You will now receive updates for: {string.Join(", ", eventSubscriptions)}");

            return sb.ToString();
        }

        public bool ShouldHandle(AppMentionEvent @event) => @event.Text.Contains("subscribe");

        public (string, string) GetHelpDescription()
        {
            var sb = new StringBuilder();

            sb.Append("Update what notifications fplbot should post. (");

            sb.Append($"{string.Join(", ", Enum.GetNames(typeof(EventSubscription)))})");

            return (
            "subscribe/unsubscribe {comma separated list of events}",
            sb.ToString());
        }
    }
}