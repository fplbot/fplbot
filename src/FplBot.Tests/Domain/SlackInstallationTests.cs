using FplBot.Domain;

namespace FplBot.Tests.Domain;

public class SlackInstallationTests
{
    [Fact]
    public void Install_IsActive()
    {
        var installation = SlackInstallation.Install("T1", "token");
        Assert.True(installation.IsActive);
        Assert.Equal("T1", installation.TeamId);
    }

    [Fact]
    public void Uninstall_IsNoLongerActive()
    {
        var installation = SlackInstallation.Install("T1", "token");
        installation.Uninstall();
        Assert.False(installation.IsActive);
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
        installation.Unsubscribe("C1", FplEvent.Standings);
        Assert.Empty(installation.ChannelSubscriptions);
    }
}
