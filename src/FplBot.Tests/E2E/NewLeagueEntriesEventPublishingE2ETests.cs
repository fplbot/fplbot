using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Helpers;
using Fpl.EventPublishers.States;
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
        await fixture.Subscribe(_teamId, _slackChannel, FplEvent.PriceChanges);
        await fixture.AskSlackbot(_teamId, _slackChannel, $"<@UREFQD887> follow {_leagueId}");
        await fixture.SlackCapture.WaitForMessageAsync(_slackChannel);
        fixture.SlackCapture.Reset();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task OnGameweekTransition_PostsNewEntriesToSlack()
    {
        await ApproachDeadline();

        var msg = await fixture.SlackCapture.WaitForMessageAsync(_slackChannel);
        Assert.Contains("New entry in", msg.Text);
        Assert.Contains("John Korsnes (Takk for meg)", msg.Text);
    }

    [Fact]
    public async Task OnGameweekTransition_PostsNewEntriesToDiscord()
    {
        var guild = await fixture.SeedGuildInstallation(_leagueId);
        var guildChannel = guild.ChannelSubscriptions.First().ChannelId;

        await ApproachDeadline();

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(guildChannel);
        Assert.Contains("New league entry", msg.Title);
        Assert.Contains("John Korsnes (Takk for meg)", msg.Description);
    }

    [Fact]
    public async Task WhenLeagueHasNoNewEntries_PostsNothing()
    {
        StubLeague(WithNewEntries());

        await ApproachDeadline();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(_slackChannel, TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task WhenChannelFollowsNoLeague_PostsNothing()
    {
        var otherChannel = "#nofollow-" + Guid.NewGuid().ToString("N")[..8];
        await fixture.Subscribe(_teamId, otherChannel, FplEvent.PriceChanges);

        await ApproachDeadline();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(otherChannel, TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task WhenManyJoined_ListsFiveAndSaysBunchMore()
    {
        StubLeague(WithNewEntries(Enumerable.Range(1, 8)
            .Select(i => Entrant("Player", i.ToString(), $"Team {i}")).ToArray()));

        await ApproachDeadline();

        var msg = await fixture.SlackCapture.WaitForMessageAsync(_slackChannel);
        Assert.Contains("Player 5 (Team 5)", msg.Text);
        Assert.DoesNotContain("Player 6", msg.Text);
        Assert.Contains("along with a bunch more", msg.Text);
    }

    [Fact]
    public async Task OnFirstGameweek_PostsNothing()
    {
        await ApproachDeadline(gameweekId: 1);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(_slackChannel, TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task OnLeaguesOwnFirstGameweek_PostsNothing()
    {
        var league = WithNewEntries(Entrant("John", "Korsnes", "Takk for meg"));
        league.Properties!.StartEvent = 5;
        StubLeague(league);

        await ApproachDeadline();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(_slackChannel, TimeSpan.FromMilliseconds(500)));
    }

    private async Task ApproachDeadline(int gameweekId = 5)
    {
        var deadline = new DateTime(2026, 9, 18, 17, 30, 0, DateTimeKind.Utc);
        var settings = new GlobalSettings
        {
            Gameweeks = [new Gameweek { Id = gameweekId, IsCurrent = false, IsNext = true, Deadline = deadline }]
        };
        var monitor = new NearDeadLineMonitor(
            GlobalSettingsClientBuilder.Returning(settings),
            new DateTimeUtils { NowUtcOverride = deadline.AddHours(-1) },
            fixture.Services.GetRequiredService<IServiceScopeFactory>(),
            A.Fake<ILogger<NearDeadLineMonitor>>());

        await monitor.EveryMinuteTick();
    }

    private void StubLeague(ClassicLeague league) =>
        A.CallTo(() => fixture.Services.GetRequiredService<ILeagueClient>()
                .GetClassicLeague(_leagueId, A<int>._, A<bool>._, A<int?>._))
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
}
