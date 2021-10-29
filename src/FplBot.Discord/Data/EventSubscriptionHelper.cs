using System;
using System.Collections.Generic;
using System.Linq;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Discord.Data
{
    public static class EventSubscriptionHelper
    {
        public static IEnumerable<EventSubscription> GetAllSubscriptionTypes()
        {
            return Enum.GetValues(typeof(EventSubscription)).Cast<EventSubscription>();
        }


        public static bool ContainsSubscriptionFor(this IEnumerable<EventSubscription> eventSubscriptions, EventSubscription eventSubscription)
        {
            var events = eventSubscriptions as EventSubscription[] ?? eventSubscriptions.ToArray();

            return events.Contains(EventSubscription.All) || events.Contains(eventSubscription);
        }

        public static bool ContainsStat(this IEnumerable<EventSubscription> subscriptions, StatType statType)
        {
            var subscriptionType = statType.GetSubscriptionType();
            return subscriptionType.HasValue && subscriptions.ContainsSubscriptionFor(subscriptionType.Value);
        }

        public static EventSubscription? GetSubscriptionType(this StatType statType)
        {
            return statType switch
            {
                StatType.GoalsScored => EventSubscription.FixtureGoals,
                StatType.Assists => EventSubscription.FixtureAssists,
                StatType.OwnGoals => EventSubscription.FixtureGoals,
                StatType.RedCards => EventSubscription.FixtureCards,
                StatType.PenaltiesSaved => EventSubscription.FixturePenaltyMisses,
                StatType.PenaltiesMissed => EventSubscription.FixturePenaltyMisses,
                _ => null
            };
        }

    }
}
