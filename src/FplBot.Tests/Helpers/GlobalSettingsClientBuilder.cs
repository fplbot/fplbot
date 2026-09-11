using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;

namespace FplBot.Tests.Helpers;

public static class GlobalSettingsClientBuilder
{
    /// <summary>
    /// Fakes IGlobalSettingsClient.GetGlobalSettings() to return the given values in order,
    /// repeating the last value on every subsequent call. A single argument behaves like a
    /// plain, unconditional Returns(); two or more model the .Once().Then.Returns(...) pattern
    /// used to simulate state changing between polling ticks.
    /// </summary>
    public static IGlobalSettingsClient Returning(params GlobalSettings?[] sequence)
    {
        var fake = A.Fake<IGlobalSettingsClient>();
        var callCount = 0;
        A.CallTo(() => fake.GetGlobalSettings()).ReturnsLazily(() =>
        {
            var index = Math.Min(callCount, sequence.Length - 1);
            callCount++;
            return sequence[index];
        });
        return fake;
    }
}
