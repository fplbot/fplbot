using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Slack.Data.Models;

namespace FplBot.Slack.Extensions;

public static class EventSubscriptionExtensions
{
    public static bool ContainsSubscriptionFor(this IEnumerable<EventSubscription> eventSubscriptions,
        EventSubscription eventSubscription)
    {
        var events = eventSubscriptions as EventSubscription[] ?? eventSubscriptions.ToArray();

        return events.Contains(EventSubscription.All) || events.Contains(eventSubscription);
    }

    public static bool ContainsStat(this IEnumerable<EventSubscription> subscriptions, StatType statType)
    {
        var subscriptionType = statType.GetSubscriptionType();
        return subscriptionType.HasValue && subscriptions.ContainsSubscriptionFor(subscriptionType.Value);
    }
}

public static class EventSubscriptionHelper
{
    public static IEnumerable<EventSubscription> GetAllSubscriptionTypes()
    {
        return Enum.GetValues(typeof(EventSubscription)).Cast<EventSubscription>();
    }
}