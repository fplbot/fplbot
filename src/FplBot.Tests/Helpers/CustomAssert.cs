using System;
using System.Collections.Generic;
using System.Linq;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Xunit.Sdk;

namespace FplBot.Tests.Helpers
{
    public static class CustomAssert
    {
        public static void AnyOfContains(IEnumerable<string> collectionOfPossibleSubstrings, string actualString)
        {
            var possibleSubstrings = collectionOfPossibleSubstrings.MaterializeToArray();
            if (possibleSubstrings.Any(
                possibleSubstring => actualString != null && actualString.IndexOf(possibleSubstring, StringComparison.CurrentCulture) >= 0))
            {
                return;
            }

            throw new ContainsException(string.Join("\n", possibleSubstrings), actualString);
        }
    }
}