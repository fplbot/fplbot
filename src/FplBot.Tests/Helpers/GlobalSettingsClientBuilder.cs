using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;

namespace FplBot.Tests.Helpers;

public static class GlobalSettingsClientBuilder
{
    /// <summary>
    /// Fakes IGlobalSettingsClient.GetGlobalSettings() to always return the given value.
    /// </summary>
    public static IGlobalSettingsClient Returning(GlobalSettings? settings)
    {
        var fake = A.Fake<IGlobalSettingsClient>();
        A.CallTo(() => fake.GetGlobalSettings()).Returns(settings);
        return fake;
    }

    /// <summary>
    /// Fakes IGlobalSettingsClient.GetGlobalSettings() to return <paramref name="first"/> on the
    /// first call and <paramref name="second"/> on every call after that — models state changing
    /// between polling ticks.
    /// </summary>
    public static IGlobalSettingsClient Returning(GlobalSettings? first, GlobalSettings? second)
    {
        var fake = A.Fake<IGlobalSettingsClient>();
        A.CallTo(() => fake.GetGlobalSettings())
            .Returns(first).Once()
            .Then.Returns(second);
        return fake;
    }
}
