using FplBot.Domain;

namespace FplBot.Tests.UnitTests.Domain;

public class InstallationTests
{
    [Fact]
    public void Install_CreatesInstallationWithEmptySubs()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        Assert.Equal("T1", installation.Id);
        Assert.Equal("Team One", installation.Name);
        Assert.Empty(installation.ChannelSubscriptions);
    }

    [Fact]
    public void Install_TokenLess_CreatesInstallationWithoutToken()
    {
        var installation = Installation.Install("G1", "Guild One");
        Assert.Equal("G1", installation.Id);
        Assert.Equal("Guild One", installation.Name);
        Assert.Null(installation.Token);
    }

    [Fact]
    public void Reinstall_TokenLess_PreservesExistingChannelsWithoutToken()
    {
        var existingChannel = ChannelSubscription.Load("C1", new ClassicLeagueId(42), [FplEvent.Standings]);

        var installation = Installation.Reinstall("G1", "Guild One", [existingChannel]);

        Assert.Equal("G1", installation.Id);
        Assert.Equal("Guild One", installation.Name);
        Assert.Null(installation.Token);
        var channel = Assert.Single(installation.ChannelSubscriptions);
        Assert.Equal("C1", channel.ChannelId);
        Assert.True(channel.IsSubscribedTo(FplEvent.Standings));
    }

    [Fact]
    public void Uninstall_ClearsToken()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Uninstall();
        Assert.Null(installation.Token);
    }

    [Fact]
    public void Uninstall_RemovesSubs()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Subscribe("C1", [FplEvent.Standings]);
        installation.Uninstall();
        Assert.Empty(installation.ChannelSubscriptions);
    }

    [Fact]
    public void MarkForRemoval_SetsPendingRemoval()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.MarkForRemoval();
        Assert.True(installation.PendingRemoval);
    }

    [Fact]
    public void MarkForRemoval_LeavesTokenAndSubsUntouched()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Subscribe("C1", [FplEvent.Standings]);
        installation.MarkForRemoval();

        Assert.Equal("token", installation.Token);
        var channel = Assert.Single(installation.ChannelSubscriptions);
        Assert.True(channel.IsSubscribedTo(FplEvent.Standings));
    }

    [Fact]
    public void Follow_OnNewChannel_CreatesAChannelSubscription()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        var leagueId = new ClassicLeagueId(42);

        installation.Follow("C1", leagueId);

        var channel = Assert.Single(installation.ChannelSubscriptions);
        Assert.Equal("C1", channel.ChannelId);
        Assert.Equal(leagueId, channel.FollowedLeagueId);
    }

    [Fact]
    public void Follow_OnAlreadyKnownChannel_UpdatesItInsteadOfDuplicating()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Follow("C1", new ClassicLeagueId(1));
        installation.Follow("C1", new ClassicLeagueId(2));

        var channel = Assert.Single(installation.ChannelSubscriptions);
        Assert.Equal(new ClassicLeagueId(2), channel.FollowedLeagueId);
    }

    [Fact]
    public void Subscribe_OnNewChannel_CreatesAChannelSubscription()
    {
        var installation = Installation.Install("T1", "Team One", "token");

        installation.Subscribe("C1", [FplEvent.Standings]);

        var channel = Assert.Single(installation.ChannelSubscriptions);
        Assert.True(channel.IsSubscribedTo(FplEvent.Standings));
    }

    [Fact]
    public void Subscribe_OnAlreadyKnownChannel_AddsToExistingRatherThanDuplicating()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Follow("C1", new ClassicLeagueId(1));
        installation.Subscribe("C1", [FplEvent.Standings]);

        var channel = Assert.Single(installation.ChannelSubscriptions);
        Assert.Equal(new ClassicLeagueId(1), channel.FollowedLeagueId);
        Assert.True(channel.IsSubscribedTo(FplEvent.Standings));
    }

    [Fact]
    public void Subscribe_DifferentChannels_TrackedSeparately()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Subscribe("C1", [FplEvent.Standings]);
        installation.Subscribe("C2", [FplEvent.Deadlines]);

        Assert.Equal(2, installation.ChannelSubscriptions.Count);
    }

    [Fact]
    public void Unsubscribe_OnUnknownChannel_DoesNotCreateOne()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Unsubscribe("C1", [FplEvent.Standings]);
        Assert.Empty(installation.ChannelSubscriptions);
    }

    [Fact]
    public void Subscribe_Many_CreatesSubscriptionWithAllOfThem()
    {
        var installation = Installation.Install("T1", "Team One", "token");

        installation.Subscribe("C123", [FplEvent.Standings, FplEvent.Deadlines]);

        var subscription = Assert.Single(installation.ChannelSubscriptions);
        Assert.True(subscription.IsSubscribedTo(FplEvent.Standings));
        Assert.True(subscription.IsSubscribedTo(FplEvent.Deadlines));
    }

    [Fact]
    public void Follow_CalledAgain_ChangesTheFollowedLeague()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Follow("C123", new ClassicLeagueId(1));
        installation.Follow("C123", new ClassicLeagueId(2));

        var subscription = Assert.Single(installation.ChannelSubscriptions);
        Assert.Equal(new ClassicLeagueId(2), subscription.FollowedLeagueId);
    }

    [Fact]
    public void Unsubscribe_RemovesTheEvent()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Subscribe("C123", [FplEvent.Standings]);
        installation.Unsubscribe("C123", FplEvent.Standings);

        var subscription = Assert.Single(installation.ChannelSubscriptions);
        Assert.False(subscription.IsSubscribedTo(FplEvent.Standings));
    }

    [Fact]
    public void IsSubscribedTo_WhenSubscribedToAll_IsTrueForEverything()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Subscribe("C123", [FplEvent.All]);

        var subscription = Assert.Single(installation.ChannelSubscriptions);
        Assert.True(subscription.IsSubscribedTo(FplEvent.Deadlines));
        Assert.True(subscription.IsSubscribedTo(FplEvent.Standings));
    }

    [Fact]
    public void Subscribe_All_ResultsInASingleAllEvent()
    {
        var installation = Installation.Install("T1", "Team One", "token");

        installation.Subscribe("C1", [FplEvent.All]);

        var subscription = Assert.Single(installation.ChannelSubscriptions);
        var evt = Assert.Single(subscription.Events.Current);
        Assert.Equal(FplEvent.All, evt);
    }

    [Fact]
    public void Unsubscribe_All_FromSpecificEvents_ResultsInZeroEvents()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Subscribe("C1", [FplEvent.Standings, FplEvent.Deadlines]);

        installation.Unsubscribe("C1", FplEvent.All);

        var subscription = Assert.Single(installation.ChannelSubscriptions);
        Assert.Empty(subscription.Events.Current);
    }

    [Fact]
    public void Unsubscribe_All_FromAll_ResultsInZeroEvents()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Subscribe("C1", [FplEvent.All]);

        installation.Unsubscribe("C1", FplEvent.All);

        var subscription = Assert.Single(installation.ChannelSubscriptions);
        Assert.Empty(subscription.Events.Current);
    }

    [Fact]
    public void Subscribe_CalledAgainOnSameChannel_AppendsToExistingEvents()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Subscribe("C1", [FplEvent.Standings]);
        installation.Subscribe("C1", [FplEvent.Deadlines]);

        var subscription = Assert.Single(installation.ChannelSubscriptions);
        Assert.True(subscription.IsSubscribedTo(FplEvent.Standings));
        Assert.True(subscription.IsSubscribedTo(FplEvent.Deadlines));
    }

    [Fact]
    public void Subscribe_ToAll_AfterASpecificEvent_CollapsesToJustAll()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Subscribe("C1", [FplEvent.Standings]);
        installation.Subscribe("C1", [FplEvent.All]);

        var subscription = Assert.Single(installation.ChannelSubscriptions);
        Assert.True(subscription.IsSubscribedTo(FplEvent.All));
    }

    [Fact]
    public void Subscribe_ToAll_ThenRemoveOne_SubscribesToKnownButTheRemovedOne()
    {
        var installation = Installation.Install("T1", "Team One", "token");
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

    [Fact]
    public void Unfollow_ClearsTheLeagueAndKeepsEventsThatDoNotNeedOne()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Follow("C1", new ClassicLeagueId(42));
        installation.Subscribe("C1", [FplEvent.PriceChanges, FplEvent.Deadlines]);

        installation.Unfollow("C1");

        var subscription = installation.GetChannel("C1")!;
        Assert.Null(subscription.FollowedLeagueId);
        Assert.True(subscription.IsSubscribedTo(FplEvent.PriceChanges));
        Assert.True(subscription.IsSubscribedTo(FplEvent.Deadlines));
    }

    [Fact]
    public void Unfollow_RemovesEveryEventThatNeedsALeague()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Follow("C1", new ClassicLeagueId(42));
        installation.Subscribe("C1", [FplEvent.Standings, FplEvent.Captains, FplEvent.Transfers, FplEvent.Taunts, FplEvent.Lineups]);

        installation.Unfollow("C1");

        var subscription = installation.GetChannel("C1")!;
        Assert.All(FplEvents.RequiringALeague, e => Assert.False(subscription.IsSubscribedTo(e)));
        Assert.True(subscription.IsSubscribedTo(FplEvent.Lineups));
    }

    [Fact]
    public void Unfollow_OnAnAllSubscribedChannel_ExpandsAllIntoTheEventsThatDoNotNeedALeague()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Follow("C1", new ClassicLeagueId(42));

        installation.Unfollow("C1");

        var subscription = installation.GetChannel("C1")!;
        Assert.All(FplEvents.RequiringALeague, e => Assert.False(subscription.IsSubscribedTo(e)));
        Assert.True(subscription.IsSubscribedTo(FplEvent.PriceChanges));
        Assert.True(subscription.IsSubscribedTo(FplEvent.Deadlines));
        Assert.DoesNotContain(FplEvent.All, subscription.Events.Current);
    }

    [Fact]
    public void Unfollow_OnUnknownChannel_DoesNothing()
    {
        var installation = Installation.Install("T1", "Team One", "token");

        installation.Unfollow("C1");

        Assert.Empty(installation.ChannelSubscriptions);
    }

    [Fact]
    public void MoveChannel_MovesLeagueAndEventsToTheNewChannelId()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Follow("C1", new ClassicLeagueId(42));
        installation.Subscribe("C1", [FplEvent.Deadlines]);

        var outcome = installation.MoveChannel("C1", "C2");

        Assert.Equal(MoveChannelOutcome.Moved, outcome);
        var subscription = Assert.Single(installation.ChannelSubscriptions);
        Assert.Equal("C2", subscription.ChannelId);
        Assert.Equal(new ClassicLeagueId(42), subscription.FollowedLeagueId);
        Assert.True(subscription.IsSubscribedTo(FplEvent.Deadlines));
    }

    [Fact]
    public void MoveChannel_RemovesTheOldChannelId()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Subscribe("C1", [FplEvent.Standings]);

        installation.MoveChannel("C1", "C2");

        Assert.Null(installation.GetChannel("C1"));
    }

    [Fact]
    public void MoveChannel_OnUnknownChannel_DoesNothing()
    {
        var installation = Installation.Install("T1", "Team One", "token");

        var outcome = installation.MoveChannel("C1", "C2");

        Assert.Equal(MoveChannelOutcome.SourceNotFound, outcome);
        Assert.Empty(installation.ChannelSubscriptions);
    }

    [Fact]
    public void MoveChannel_OntoAChannelThatAlreadyHasASubscription_IsRefused()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Follow("C1", new ClassicLeagueId(42));
        installation.Follow("C2", new ClassicLeagueId(99));

        var outcome = installation.MoveChannel("C1", "C2");

        Assert.Equal(MoveChannelOutcome.TargetAlreadySubscribed, outcome);
        Assert.Equal(new ClassicLeagueId(42), installation.GetChannel("C1")!.FollowedLeagueId);
        Assert.Equal(new ClassicLeagueId(99), installation.GetChannel("C2")!.FollowedLeagueId);
    }

    [Fact]
    public void MoveChannel_OntoItself_IsRefused()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Subscribe("C1", [FplEvent.Standings]);

        var outcome = installation.MoveChannel("C1", "C1");

        Assert.Equal(MoveChannelOutcome.TargetAlreadySubscribed, outcome);
        Assert.NotNull(installation.GetChannel("C1"));
    }

    [Fact]
    public void RemoveChannel_RemovesTheChannel()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Subscribe("C1", [FplEvent.Standings]);

        installation.RemoveChannel("C1");

        Assert.Empty(installation.ChannelSubscriptions);
    }

    [Fact]
    public void RemoveChannel_OnUnknownChannel_DoesNothing()
    {
        var installation = Installation.Install("T1", "Team One", "token");
        installation.Subscribe("C1", [FplEvent.Standings]);

        installation.RemoveChannel("C2");

        Assert.Single(installation.ChannelSubscriptions);
    }
}
