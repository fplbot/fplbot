using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.States;
using FplBot.Data;
using FplBot.Domain;
using FplBot.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FplBot.Tests.E2E;

[Collection("App")]
public class NewLeagueEntriesEventPublishingE2ETests(AppFixture fixture) : IAsyncLifetime
{
    private string _teamId = null!;
    private string _slackChannel = null!;
    private int _leagueId;

    public async ValueTask InitializeAsync()
    {
        fixture.SlackCapture.Reset();
        fixture.DiscordCapture.Reset();
        await fixture.FlushRedisAsync();

        _leagueId = Random.Shared.Next(100000, 999999);
        _slackChannel = "#new-entries-" + Guid.NewGuid().ToString("N")[..8];

        StubLeague(WithNewEntries(Entrant("John", "Korsnes", "Takk for meg")));

        _teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(_teamId, _slackChannel, FplEvent.NewLeagueEntries);
        await fixture.AskSlackbot(_teamId, _slackChannel, $"<@UREFQD887> follow {_leagueId}");
        await fixture.SlackCapture.WaitForMessageAsync(_slackChannel);
        fixture.SlackCapture.Reset();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task OnGameweekTransition_PostsNewEntriesToSlack()
    {
        await TransitionGameweek();

        var msg = await fixture.SlackCapture.WaitForMessageAsync(_slackChannel);
        Assert.Contains("New entry in", msg.Text);
        Assert.Contains("John Korsnes (Takk for meg)", msg.Text);
    }

    [Fact]
    public async Task OnGameweekTransition_PostsNewEntriesToDiscord()
    {
        var guild = await fixture.SeedGuildInstallation(_leagueId, [EventSubscription.NewLeagueEntries]);
        var guildChannel = guild.ChannelSubscriptions.First().ChannelId;

        await TransitionGameweek();

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(guildChannel);
        Assert.Contains("New league entry", msg.Title);
        Assert.Contains("John Korsnes (Takk for meg)", msg.Description);
    }

    [Fact]
    public async Task WhenLeagueHasNoNewEntries_PostsNothing()
    {
        StubLeague(WithNewEntries());

        await TransitionGameweek();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(_slackChannel, TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task WhenChannelNotSubscribed_PostsNothing()
    {
        var otherChannel = "#unsubscribed-" + Guid.NewGuid().ToString("N")[..8];
        await fixture.Subscribe(_teamId, otherChannel, FplEvent.PriceChanges);
        await fixture.AskSlackbot(_teamId, otherChannel, $"<@UREFQD887> follow {_leagueId}");
        await fixture.SlackCapture.WaitForMessageAsync(otherChannel);
        fixture.SlackCapture.Reset();

        await TransitionGameweek();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(otherChannel, TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task WhenManyJoined_ListsFiveAndSaysBunchMore()
    {
        StubLeague(WithNewEntries(Enumerable.Range(1, 8)
            .Select(i => Entrant("Player", i.ToString(), $"Team {i}")).ToArray()));

        await TransitionGameweek();

        var msg = await fixture.SlackCapture.WaitForMessageAsync(_slackChannel);
        Assert.Contains("Player 5 (Team 5)", msg.Text);
        Assert.DoesNotContain("Player 6", msg.Text);
        Assert.Contains("along with a bunch more", msg.Text);
    }

    [Fact]
    public async Task OnFirstGameweek_PostsNothing()
    {
        var monitor = BuildMonitor(GlobalSettingsClientBuilder.Returning(Preseason(), Gameweek1Current()));
        await monitor.EveryOtherMinuteTick(CancellationToken.None);
        await monitor.EveryOtherMinuteTick(CancellationToken.None);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(_slackChannel, TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task OnLeaguesOwnFirstGameweek_PostsNothing()
    {
        var league = WithNewEntries(Entrant("John", "Korsnes", "Takk for meg"));
        league.Properties!.StartEvent = 3;
        StubLeague(league);

        await TransitionGameweek();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(_slackChannel, TimeSpan.FromMilliseconds(500)));
    }

    private async Task TransitionGameweek()
    {
        var monitor = BuildMonitor(GlobalSettingsClientBuilder.Returning(BeforeTransition(), AfterTransition()));
        await monitor.EveryOtherMinuteTick(CancellationToken.None);
        await monitor.EveryOtherMinuteTick(CancellationToken.None);
    }

    private void StubLeague(ClassicLeague league) =>
        A.CallTo(() => fixture.Services.GetRequiredService<ILeagueClient>()
                .GetClassicLeague(_leagueId, A<int>._, A<bool>._))
            .Returns(league);

    private ClassicLeague WithNewEntries(params NewLeagueEntry[] entries) =>
        new()
        {
            Properties = new ClassicLeagueProperties { Name = "YOLO league", StartEvent = 1 },
            Standings = new ClassicLeagueStandings { Entries = [] },
            NewEntries = new NewLeagueEntries { Entries = entries, HasNext = false }
        };

    private static NewLeagueEntry Entrant(string firstName, string lastName, string entryName) =>
        new()
        {
            Entry = Random.Shared.Next(1, 999999),
            PlayerFirstName = firstName,
            PlayerLastName = lastName,
            EntryName = entryName,
            JoinedAt = DateTime.UtcNow.AddDays(-1)
        };

    private GameweekLifecycleMonitor BuildMonitor(IGlobalSettingsClient gameweekClient) =>
        new(gameweekClient,
            A.Fake<ILogger<GameweekLifecycleMonitor>>(),
            fixture.Services.GetRequiredService<IServiceScopeFactory>(),
            A.Fake<IFixtureState>(),
            A.Fake<ILineupState>());

    private static GlobalSettings BeforeTransition() => new()
    {
        Gameweeks =
        [
            TestBuilder.PreviousGameweek(1),
            TestBuilder.CurrentGameweek(2),
            TestBuilder.NextGameweek(3)
        ]
    };

    private static GlobalSettings AfterTransition() => new()
    {
        Gameweeks =
        [
            TestBuilder.OlderGameweek(1),
            TestBuilder.PreviousGameweek(2),
            TestBuilder.CurrentGameweek(3)
        ]
    };

    private static GlobalSettings Preseason() => new()
    {
        Gameweeks =
        [
            new Gameweek { Id = 1, IsCurrent = false, IsNext = true },
            new Gameweek { Id = 2 }
        ]
    };

    private static GlobalSettings Gameweek1Current() => new()
    {
        Gameweeks =
        [
            TestBuilder.CurrentGameweek(1),
            TestBuilder.NextGameweek(2)
        ]
    };
}
