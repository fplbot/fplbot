using FplBot.Data;
using FplBot.Data.Slack;
using FplBot.Domain;

namespace FplBot.Tests.Data.Slack;

public class SlackInstallationMapperTests
{
    [Fact]
    public void ToDomain_NoChannelSet_ProducesInstallationWithNoChannelSubscriptions()
    {
        var team = new SlackTeam { TeamId = "T1", TeamName = "Team One", AccessToken = "token1" };

        var installation = SlackInstallationMapper.ToDomain(team);

        Assert.Equal("T1", installation.TeamId);
        Assert.Equal("token1", installation.Token);
        Assert.True(installation.IsActive);
        Assert.Empty(installation.ChannelSubscriptions);
    }

    [Fact]
    public void ToDomain_ChannelLeagueAndSubscriptionsSet_ProducesMatchingChannelSubscription()
    {
        var team = new SlackTeam
        {
            TeamId = "T1",
            TeamName = "Team One",
            AccessToken = "token1",
            FplBotSlackChannel = "#fpl",
            FplbotLeagueId = 42,
            Subscriptions = new List<EventSubscription> { EventSubscription.Standings, EventSubscription.Deadlines }
        };

        var installation = SlackInstallationMapper.ToDomain(team);

        var channel = Assert.Single(installation.ChannelSubscriptions);
        Assert.Equal("#fpl", channel.ChannelId);
        Assert.Equal(new ClassicLeagueId(42), channel.FollowedLeagueId);
        Assert.True(channel.IsSubscribedTo(FplEvent.Standings));
        Assert.True(channel.IsSubscribedTo(FplEvent.Deadlines));
        Assert.False(channel.IsSubscribedTo(FplEvent.Captains));
    }

    [Fact]
    public void ToDomain_SubscribedToAll_IsSubscribedToEverything()
    {
        var team = new SlackTeam
        {
            TeamId = "T1",
            AccessToken = "token1",
            FplBotSlackChannel = "#fpl",
            FplbotLeagueId = 1,
            Subscriptions = new List<EventSubscription> { EventSubscription.All }
        };

        var installation = SlackInstallationMapper.ToDomain(team);

        var channel = Assert.Single(installation.ChannelSubscriptions);
        Assert.True(channel.IsSubscribedTo(FplEvent.Captains));
        Assert.True(channel.IsSubscribedTo(FplEvent.Deadlines));
    }

    [Fact]
    public void ToStorage_NoChannelFollowed_ProducesTeamWithNoChannelOrSubscriptions()
    {
        var installation = SlackInstallation.Install("T1", "token1");

        var team = SlackInstallationMapper.ToStorage(installation, "Team One");

        Assert.Equal("T1", team.TeamId);
        Assert.Equal("Team One", team.TeamName);
        Assert.Equal("token1", team.AccessToken);
        Assert.Null(team.FplBotSlackChannel);
        Assert.Null(team.FplbotLeagueId);
        Assert.Empty(team.Subscriptions);
    }

    [Fact]
    public void ToStorage_ChannelFollowedAndSubscribed_ProducesMatchingTeam()
    {
        var installation = SlackInstallation.Install("T1", "token1");
        installation.Follow("#fpl", new ClassicLeagueId(42));
        installation.Subscribe("#fpl", [FplEvent.Standings, FplEvent.Deadlines]);

        var team = SlackInstallationMapper.ToStorage(installation, "Team One");

        Assert.Equal("#fpl", team.FplBotSlackChannel);
        Assert.Equal(42, team.FplbotLeagueId);
        Assert.Contains(EventSubscription.Standings, team.Subscriptions);
        Assert.Contains(EventSubscription.Deadlines, team.Subscriptions);
    }

    [Fact]
    public void RoundTrip_ToDomainThenToStorage_PreservesChannelLeagueAndSubscriptions()
    {
        var team = new SlackTeam
        {
            TeamId = "T1",
            TeamName = "Team One",
            AccessToken = "token1",
            FplBotSlackChannel = "#fpl",
            FplbotLeagueId = 42,
            Subscriptions = new List<EventSubscription> { EventSubscription.Standings, EventSubscription.Deadlines }
        };

        var roundTripped = SlackInstallationMapper.ToStorage(SlackInstallationMapper.ToDomain(team), team.TeamName);

        Assert.Equal(team.TeamId, roundTripped.TeamId);
        Assert.Equal(team.TeamName, roundTripped.TeamName);
        Assert.Equal(team.AccessToken, roundTripped.AccessToken);
        Assert.Equal(team.FplBotSlackChannel, roundTripped.FplBotSlackChannel);
        Assert.Equal(team.FplbotLeagueId, roundTripped.FplbotLeagueId);
        Assert.Equal(team.Subscriptions.OrderBy(e => e), roundTripped.Subscriptions.OrderBy(e => e));
    }
}
