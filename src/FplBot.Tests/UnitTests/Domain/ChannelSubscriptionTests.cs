using FplBot.Domain;

namespace FplBot.Tests.UnitTests.Domain;

public class ChannelSubscriptionTests
{
    private static readonly DateTimeOffset Day0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static ChannelSubscription NewSubscription() =>
        ChannelSubscription.Subscribe("C123", [FplEvent.FixtureGoals]);

    [Fact]
    public void NewSubscription_HasNoFailures()
    {
        var sub = NewSubscription();

        Assert.Equal(0, sub.FailureCount);
        Assert.Null(sub.FailingSince);
        Assert.False(sub.IsStale(Day0));
    }

    [Fact]
    public void FirstFailure_StampsFailingSince()
    {
        var sub = NewSubscription();

        sub.RecordDeliveryFailure(Day0);

        Assert.Equal(1, sub.FailureCount);
        Assert.Equal(Day0, sub.FailingSince);
    }

    [Fact]
    public void SubsequentFailures_KeepOriginalFailingSince()
    {
        var sub = NewSubscription();

        sub.RecordDeliveryFailure(Day0);
        sub.RecordDeliveryFailure(Day0.AddDays(3));

        Assert.Equal(2, sub.FailureCount);
        Assert.Equal(Day0, sub.FailingSince);
    }

    [Fact]
    public void ClearDeliveryFailures_ResetsBothFields()
    {
        var sub = NewSubscription();
        sub.RecordDeliveryFailure(Day0);
        sub.RecordDeliveryFailure(Day0.AddDays(1));

        sub.ClearDeliveryFailures();

        Assert.Equal(0, sub.FailureCount);
        Assert.Null(sub.FailingSince);
    }

    [Fact]
    public void EnoughFailuresButTooRecent_IsNotStale()
    {
        var sub = NewSubscription();
        for (var i = 0; i < 5; i++)
        {
            sub.RecordDeliveryFailure(Day0.AddHours(i));
        }

        Assert.False(sub.IsStale(Day0.AddDays(6)));
    }

    [Fact]
    public void OldEnoughButTooFewFailures_IsNotStale()
    {
        var sub = NewSubscription();
        for (var i = 0; i < 4; i++)
        {
            sub.RecordDeliveryFailure(Day0.AddDays(i));
        }

        Assert.False(sub.IsStale(Day0.AddDays(8)));
    }

    [Fact]
    public void EnoughFailuresAndOldEnough_IsStale()
    {
        var sub = NewSubscription();
        for (var i = 0; i < 5; i++)
        {
            sub.RecordDeliveryFailure(Day0.AddDays(i));
        }

        Assert.True(sub.IsStale(Day0.AddDays(8)));
    }

    [Fact]
    public void ClearedAfterReachingThreshold_IsNotStale()
    {
        var sub = NewSubscription();
        for (var i = 0; i < 5; i++)
        {
            sub.RecordDeliveryFailure(Day0.AddDays(i));
        }

        sub.ClearDeliveryFailures();

        Assert.False(sub.IsStale(Day0.AddDays(8)));
    }

    [Fact]
    public void Load_RestoresFailureState()
    {
        var sub = ChannelSubscription.Load("C123", null, [FplEvent.FixtureGoals], failureCount: 4, failingSince: Day0);

        Assert.Equal(4, sub.FailureCount);
        Assert.Equal(Day0, sub.FailingSince);
    }
}
