using FplBot.Domain;

namespace FplBot.Tests.Domain;

public class SlackInstallationTests
{
    [Fact]
    public void Install_IsActiveWithEmptySubs()
    {
        var installation = SlackInstallation.Install("T1", "token");
        Assert.True(installation.IsActive);
        Assert.Equal("T1", installation.TeamId);
        Assert.Empty(installation.ChannelSubscriptions);
    }

    [Fact]
    public void Uninstall_IsNoLongerActive()
    {
        var installation = SlackInstallation.Install("T1", "token");
        installation.Uninstall();
        Assert.False(installation.IsActive);
    }

    [Fact]
    public void Uninstall_RemovesSubs()
    {
        var installation = SlackInstallation.Install("T1", "token");
        installation.Subscribe("C1", [FplEvent.Standings]);
        installation.Uninstall();
        Assert.Empty(installation.ChannelSubscriptions);
    }

    [Fact]
    public void Follow_OnNewChannel_CreatesAChannelSubscription()
    {
        var installation = SlackInstallation.Install("T1", "token");
        var leagueId = new ClassicLeagueId(42);

        installation.Follow("C1", leagueId);

        var channel = Assert.Single(installation.ChannelSubscriptions);
        Assert.Equal("C1", channel.ChannelId);
        Assert.Equal(leagueId, channel.FollowedLeagueId);
    }

    [Fact]
    public void Follow_OnAlreadyKnownChannel_UpdatesItInsteadOfDuplicating()
    {
        var installation = SlackInstallation.Install("T1", "token");
        installation.Follow("C1", new ClassicLeagueId(1));
        installation.Follow("C1", new ClassicLeagueId(2));

        var channel = Assert.Single(installation.ChannelSubscriptions);
        Assert.Equal(new ClassicLeagueId(2), channel.FollowedLeagueId);
    }

    [Fact]
    public void Subscribe_OnNewChannel_CreatesAChannelSubscription()
    {
        var installation = SlackInstallation.Install("T1", "token");

        installation.Subscribe("C1", [FplEvent.Standings]);

        var channel = Assert.Single(installation.ChannelSubscriptions);
        Assert.True(channel.IsSubscribedTo(FplEvent.Standings));
    }

    [Fact]
    public void Subscribe_OnAlreadyKnownChannel_AddsToExistingRatherThanDuplicating()
    {
        var installation = SlackInstallation.Install("T1", "token");
        installation.Follow("C1", new ClassicLeagueId(1));
        installation.Subscribe("C1", [FplEvent.Standings]);

        var channel = Assert.Single(installation.ChannelSubscriptions);
        Assert.Equal(new ClassicLeagueId(1), channel.FollowedLeagueId);
        Assert.True(channel.IsSubscribedTo(FplEvent.Standings));
    }

    [Fact]
    public void Subscribe_DifferentChannels_TrackedSeparately()
    {
        var installation = SlackInstallation.Install("T1", "token");
        installation.Subscribe("C1", [FplEvent.Standings]);
        installation.Subscribe("C2", [FplEvent.Deadlines]);

        Assert.Equal(2, installation.ChannelSubscriptions.Count);
    }

    [Fact]
    public void Unsubscribe_OnUnknownChannel_DoesNotCreateOne()
    {
        var installation = SlackInstallation.Install("T1", "token");
        installation.Unsubscribe("C1", [FplEvent.Standings]);
        Assert.Empty(installation.ChannelSubscriptions);
    }

    [Fact]
    public void Subscribe_Many_CreatesSubscriptionWithAllOfThem()
    {
        var installation = SlackInstallation.Install("T1", "token");

        installation.Subscribe("C123", [FplEvent.Standings, FplEvent.Deadlines]);

        var subscription = Assert.Single(installation.ChannelSubscriptions);
        Assert.True(subscription.IsSubscribedTo(FplEvent.Standings));
        Assert.True(subscription.IsSubscribedTo(FplEvent.Deadlines));
    }

    [Fact]
    public void Follow_CalledAgain_ChangesTheFollowedLeague()
    {
        var installation = SlackInstallation.Install("T1", "token");
        installation.Follow("C123", new ClassicLeagueId(1));
        installation.Follow("C123", new ClassicLeagueId(2));

        var subscription = Assert.Single(installation.ChannelSubscriptions);
        Assert.Equal(new ClassicLeagueId(2), subscription.FollowedLeagueId);
    }

    [Fact]
    public void Unsubscribe_RemovesTheEvent()
    {
        var installation = SlackInstallation.Install("T1", "token");
        installation.Subscribe("C123", [FplEvent.Standings]);
        installation.Unsubscribe("C123", FplEvent.Standings);

        var subscription = Assert.Single(installation.ChannelSubscriptions);
        Assert.False(subscription.IsSubscribedTo(FplEvent.Standings));
    }

    [Fact]
    public void IsSubscribedTo_WhenSubscribedToAll_IsTrueForEverything()
    {
        var installation = SlackInstallation.Install("T1", "token");
        installation.Subscribe("C123", [FplEvent.All]);

        var subscription = Assert.Single(installation.ChannelSubscriptions);
        Assert.True(subscription.IsSubscribedTo(FplEvent.Deadlines));
        Assert.True(subscription.IsSubscribedTo(FplEvent.Standings));
    }

    [Fact]
    public void Subscribe_CalledAgainOnSameChannel_AppendsToExistingEvents()
    {
        var installation = SlackInstallation.Install("T1", "token");
        installation.Subscribe("C1", [FplEvent.Standings]);
        installation.Subscribe("C1", [FplEvent.Deadlines]);

        var subscription = Assert.Single(installation.ChannelSubscriptions);
        Assert.True(subscription.IsSubscribedTo(FplEvent.Standings));
        Assert.True(subscription.IsSubscribedTo(FplEvent.Deadlines));
    }

    [Fact]
    public void Subscribe_ToAll_AfterASpecificEvent_CollapsesToJustAll()
    {
        var installation = SlackInstallation.Install("T1", "token");
        installation.Subscribe("C1", [FplEvent.Standings]);
        installation.Subscribe("C1", [FplEvent.All]);

        var subscription = Assert.Single(installation.ChannelSubscriptions);
        Assert.True(subscription.IsSubscribedTo(FplEvent.All));
    }

    [Fact]
    public void Subscribe_ToAll_ThenRemoveOne_SubscribesToKnownButTheRemovedOne()
    {
        var installation = SlackInstallation.Install("T1", "token");
        installation.Subscribe("C1", [FplEvent.All]);
        installation.Unsubscribe("C1", [FplEvent.Captains]);

        var subscription = Assert.Single(installation.ChannelSubscriptions);
        foreach (var @event in Enum.GetValues<FplEvent>())
        {
            if(@event == FplEvent.Captains)
            {
                Assert.False(subscription.IsSubscribedTo(@event));
            }
            else if(@event == FplEvent.All)
            {
                Assert.False(subscription.IsSubscribedTo(@event));
            }
            else
            {
                Assert.True(subscription.IsSubscribedTo(@event));
            }
        }
    }
}
