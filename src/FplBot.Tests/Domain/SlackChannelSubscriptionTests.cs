using FplBot.Domain;

namespace FplBot.Tests.Domain;

public class SlackChannelSubscriptionTests
{
    [Fact]
    public void Follow_CreatesSubscriptionForThatChannelAndLeague()
    {
        var leagueId = new ClassicLeagueId(123);
        var subscription = SlackChannelSubscription.Follow("C123", leagueId);

        Assert.Equal("C123", subscription.ChannelId);
        Assert.Equal(leagueId, subscription.FollowedLeagueId);
        Assert.Empty(subscription.Events.Current);
    }

    [Fact]
    public void Subscribe_Single_CreatesSubscriptionWithThatEvent()
    {
        var subscription = SlackChannelSubscription.Subscribe("C123", FplEvent.Standings);

        Assert.Equal("C123", subscription.ChannelId);
        Assert.Null(subscription.FollowedLeagueId);
        Assert.True(subscription.IsSubscribedTo(FplEvent.Standings));
    }

    [Fact]
    public void Subscribe_Many_CreatesSubscriptionWithAllOfThem()
    {
        var subscription = SlackChannelSubscription.Subscribe("C123", [FplEvent.Standings, FplEvent.Deadlines]);

        Assert.True(subscription.IsSubscribedTo(FplEvent.Standings));
        Assert.True(subscription.IsSubscribedTo(FplEvent.Deadlines));
    }

    [Fact]
    public void Follow_CalledAgain_ChangesTheFollowedLeague()
    {
        var subscription = SlackChannelSubscription.Follow("C123", new ClassicLeagueId(1));
        subscription.Follow(new ClassicLeagueId(2));
        Assert.Equal(new ClassicLeagueId(2), subscription.FollowedLeagueId);
    }

    [Fact]
    public void Unsubscribe_RemovesTheEvent()
    {
        var subscription = SlackChannelSubscription.Subscribe("C123", FplEvent.Standings);
        subscription.Unsubscribe(FplEvent.Standings);
        Assert.False(subscription.IsSubscribedTo(FplEvent.Standings));
    }

    [Fact]
    public void IsSubscribedTo_WhenSubscribedToAll_IsTrueForEverything()
    {
        var subscription = SlackChannelSubscription.Subscribe("C123", FplEvent.All);
        Assert.True(subscription.IsSubscribedTo(FplEvent.Deadlines));
        Assert.True(subscription.IsSubscribedTo(FplEvent.Standings));
    }
}
