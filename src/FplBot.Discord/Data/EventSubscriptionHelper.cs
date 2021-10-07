using System;
using System.Collections.Generic;
using System.Linq;

namespace FplBot.Discord.Data
{
    public static class EventSubscriptionHelper
    {
        public static IEnumerable<EventSubscription> GetAllSubscriptionTypes()
        {
            return Enum.GetValues(typeof(EventSubscription)).Cast<EventSubscription>();
        }
    }
}
