using System.Collections.Generic;
using System.Linq;

namespace FplBot.Data.Models
{
    internal static class Extensions
    {
        public static bool ContainsSubscriptionFor(this IEnumerable<EventSubscription> eventSubscriptions,
            EventSubscription eventSubscription)
        {
            var events = eventSubscriptions as EventSubscription[] ?? eventSubscriptions.ToArray();

            return events.Contains(EventSubscription.All) || events.Contains(eventSubscription);
        }
    }
}
