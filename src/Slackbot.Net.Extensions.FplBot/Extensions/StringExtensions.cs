using Slackbot.Net.Extensions.FplBot.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Slackbot.Net.Extensions.FplBot.Extensions
{
    public static class StringExtensions
    {
        public static string Abbreviated(this string text)
        {
            return string.Join("", text.Replace("-", " ").Split(" ").Select(s => s.First()));
        }

        public static IEnumerable<EventSubscription> ParseSubscriptionString(this string subscriptionString, string delimiter, out string[] unableToParse)
        {
            var erroneous = new List<string>();

            var parsed = string.IsNullOrWhiteSpace(subscriptionString) ? 
                Enumerable.Empty<EventSubscription>() : 
                subscriptionString.Split(delimiter).Select(s =>
                {
                    if (!Enum.TryParse(typeof(EventSubscription), s.Trim(), true, out var result))
                    {
                        erroneous.Add(s);
                    }
                    return result;
                }).Cast<EventSubscription>();

            unableToParse = erroneous.ToArray();

            return parsed;
        }
    }
}
