using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FplBot.Core.Abstractions;
using FplBot.Core.Extensions;
using FplBot.Core.Helpers;
using FplBot.Data;
using FplBot.Data.Abstractions;
using FplBot.Data.Models;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Core.Handlers
{
    internal class FplSubscribeCommandHandler : HandleAppMentionBase
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

        public override string[] Commands => new[] {"subscribe", "unsubscribe"};

        public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent appMentioned)
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
                (var inputSubscriptions, var unableToParse) = ParseSubscriptionsFromInput(appMentioned);

                if (inputSubscriptions.Count() < 1 && unableToParse.Count() < 1)
                {
                    return $"You need to pass some arguments\n {FormatAllSubsAvailable()}";
                }

                if (inputSubscriptions.Count() < 1)
                {
                    return $"I was not able to understand: *{string.Join(", ", unableToParse)}.* :confused: \n {FormatAllSubsAvailable()}";
                }

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
            if (currentSubscriptions.Contains(EventSubscription.All))
            {
                return inputSubscriptions;
            }

            if (inputSubscriptions.Contains(EventSubscription.All))
            {
                return new List<EventSubscription>() { EventSubscription.All };
            }

            return currentSubscriptions.Union(inputSubscriptions);
        }

        private static (IEnumerable<EventSubscription> events, string[] unableToParse) ParseSubscriptionsFromInput(AppMentionEvent appMentioned)
        {
            var stringListOfEvents = MessageHelper.ExtractArgs(appMentioned.Text, new []{ "subscribe {args}", "unsubscribe {args}"});
            return stringListOfEvents.ParseSubscriptionString(delimiter: ",");
        }

        private string FormatSubscriptionMessage(IEnumerable<EventSubscription> eventSubscriptions, string[] unableToParse)
        {
            var sb = new StringBuilder();

            sb.Append("Updated subscriptions :sparkles:\n");

            if (eventSubscriptions.Count() < 1) sb.Append($"You are not subscribing to any fplbot updates.");
            else sb.Append($"You will now receive updates for:\n{Formatter.BulletPoints(eventSubscriptions)}");

            if (unableToParse.Any())
            {
                sb.Append("\n");
                sb.Append($"Btw, I was not able to understand these: *{string.Join(", ", unableToParse)}.* \n" + FormatAllSubsAvailable());
            }

            return sb.ToString();
        }

        private string FormatAllSubsAvailable()
        {
            return $"You can choose from: {string.Join(", ", EventSubscriptionHelper.GetAllSubscriptionTypes())}";
        }

        public override (string, string) GetHelpDescription()
        {
            var sb = new StringBuilder();

            sb.Append("Update what notifications fplbot should post. (");

            sb.Append($"{string.Join(", ", EventSubscriptionHelper.GetAllSubscriptionTypes())})");

            return (
            "subscribe/unsubscribe {comma separated list of events}",
            sb.ToString());
        }
    }
}
