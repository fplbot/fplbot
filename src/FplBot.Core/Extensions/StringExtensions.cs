using System;
using System.Collections.Generic;
using System.Linq;
using FplBot.Core.Abstractions;

namespace FplBot.Core.Extensions
{
    public static class StringExtensions
    {
        public static string Abbreviated(this string text)
        {
            return string.Join("", text.Replace("-", " ").Split(" ").Select(s => s.First()));
        }

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

        public static string TextAfterFirstSpace(this string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return string.Empty;
            }

            var firstSpaceIndex = s.IndexOf(' ');
            if (s.Length <= firstSpaceIndex + 1)
            {
                return string.Empty;
            }

            return s.Substring(firstSpaceIndex + 1).Trim();
        }
    }
}
