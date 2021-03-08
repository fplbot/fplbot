using System;
using System.Collections.Generic;
using System.Linq;
using Fpl.Data;
using Fpl.Data.Models;
using Fpl.Data.Repositories;
using FplBot.Core.Abstractions;

namespace FplBot.Core.Extensions
{
    public static class EventSubscriptionExtensions
    {
        public static bool ContainsSubscriptionFor(this IEnumerable<EventSubscription> eventSubscriptions,
            EventSubscription eventSubscription)
        {
            var events = eventSubscriptions as EventSubscription[] ?? eventSubscriptions.ToArray();

            return events.Contains(EventSubscription.All) || events.Contains(eventSubscription);
        }
    }

    public static class EventSubscriptionHelper
    {
        public static IEnumerable<EventSubscription> GetAllSubscriptionTypes()
        {
            return Enum.GetValues(typeof(EventSubscription)).Cast<EventSubscription>();
        }
    }
}