using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                var inputSubscriptions = ParseSubscriptionsFromInput(appMentioned, out var unableToParse);

                var newSubscriptions = appMentioned.Text.Contains("unsubscribe") ?
                    UnsubscribeToEvents(inputSubscriptions, currentSubscriptions) :
                    SubscribeToEvents(inputSubscriptions, currentSubscriptions);

                await _teamRepo.UpdateSubscriptions(teamId, newSubscriptions);

                return FormatSubscriptionMessage(newSubscriptions, unableToParse);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return $"Oops, could not update subscriptions.";
            }
        }

        private IEnumerable<EventSubscription> UnsubscribeToEvents(
            IEnumerable<EventSubscription> inputSubscriptions,
            IEnumerable<EventSubscription> currentSubscriptions
        )
        {
            if (inputSubscriptions.Contains(EventSubscription.All)) { return new List<EventSubscription>(); }

            if (currentSubscriptions.Contains(EventSubscription.All))
            {
                return EventSubscriptionHelper.GetAllSubscriptionTypes().Except(inputSubscriptions.Append(EventSubscription.All));
            }

            return currentSubscriptions.Except(inputSubscriptions);
        }

        private IEnumerable<EventSubscription> SubscribeToEvents(
            IEnumerable<EventSubscription> inputSubscriptions,
            IEnumerable<EventSubscription> currentSubscriptions
        )
        {
            if (inputSubscriptions.Contains(EventSubscription.All))
            {
                return new List<EventSubscription>() { EventSubscription.All };
            }

            return currentSubscriptions.Union(inputSubscriptions);
        }

        private static IEnumerable<EventSubscription> ParseSubscriptionsFromInput(AppMentionEvent appMentioned, out string[] unableToParse)
        {
            var stringListOfEvents = MessageHelper.ExtractArgs(appMentioned.Text, "subscribe {args}");
            return stringListOfEvents.ParseSubscriptionString(delimiter: ",", out unableToParse);
        }

        private string FormatSubscriptionMessage(IEnumerable<EventSubscription> eventSubscriptions, string[] unableToParse)
        {
            var sb = new StringBuilder();

            sb.Append("Updated subscriptions :sparkles:\n");
            sb.Append($"You will now receive updates for:\n{Formatter.BulletPoints(eventSubscriptions)}");

            if (unableToParse.Any())
            {
                sb.Append("\n");
                sb.Append($"Btw, I was unable to understand these: {string.Join(", ", unableToParse)}. " +
                          $"You can choose from: {string.Join(", ", EventSubscriptionHelper.GetAllSubscriptionTypes())}");
            }

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