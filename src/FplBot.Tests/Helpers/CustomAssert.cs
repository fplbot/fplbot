using Fpl.EventPublishers.Extensions;
using Xunit.Sdk;

namespace FplBot.Tests.Helpers;

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

        throw new XunitException($"Expected actual string to contain any of:\n{string.Join("\n", possibleSubstrings)}\nActual: {actualString}");
    }
}
