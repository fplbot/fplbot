using System;
using System.Collections.Generic;
using System.Linq;
using FplBot.Core.Data.Models;

namespace FplBot.Core.Data
{
    public static class StringExtensions
    {
        public static (IEnumerable<EventSubscription> events, string[] unableToParse) ParseSubscriptionString(this string subscriptionString, string delimiter)
        {
            var events = new List<EventSubscription>();
            var erroneous = new List<string>();

            if (string.IsNullOrWhiteSpace(subscriptionString))
            {
                return (Enumerable.Empty<EventSubscription>(), Array.Empty<string>());
            }

            var split = subscriptionString.Split(delimiter);
            foreach (var s in split)
            {
                var trimmed = s.Trim();
                if (Enum.TryParse(typeof(EventSubscription), trimmed, true, out var result))
                {
                    events.Add((EventSubscription)result);
                }
                else
                {
                    erroneous.Add(trimmed);
                }
            }

            return (events, erroneous.ToArray());
        }
    }
}
