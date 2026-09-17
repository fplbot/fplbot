using Bogus;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.RecurringActions;
using FplBot.Data.Discord;
using FplBot.Data.Leagues;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FplBot.Tests.E2E.Slack.SlackSubscriptions;

[Collection("App")]
public class NewLeagueEntriesEventPublishingE2ETests(AppFixture fixture) : IAsyncLifetime
{
    private static readonly Faker Faker = new();
    private static readonly DateTime FirstJoin = new(2021, 11, 30, 11, 25, 6, DateTimeKind.Utc);

    // Sub-millisecond precision, just like the joined_time the FPL API reports.
    private static readonly DateTime SecondJoin = new DateTime(2021, 12, 1, 9, 0, 0, DateTimeKind.Utc).AddTicks(9_562_530);

    private string _teamId = null!;
    private string _channel = null!;
    private int _leagueId;

    public async ValueTask InitializeAsync()
    {
        fixture.SlackCapture.Reset();
        await fixture.FlushRedisAsync();
        _channel = "#newentries-" + Guid.NewGuid().ToString("N")[..8];
        _leagueId = Faker.Random.Int(100_000, 999_999);
        _teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(_teamId, _channel, FplEvent.NewLeagueEntries);

        // New entries are only polled for- and posted to channels following a league —
        // drive that through the real "follow" command, same as GameweekEventPublishingE2ETests.
        await fixture.AskSlackbot(_teamId, _channel, $"<@UREFQD887> follow {_leagueId}");
        await fixture.SlackCapture.WaitForMessageAsync(_channel); // "Thanks! You're now following..."
        fixture.SlackCapture.Reset();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task OnFirstPoll_DoesNotNotifyAboutEntriesJoiningBeforeFollowing()
    {
        var action = BuildAction(LeagueWith(John()));

        await action.Process(CancellationToken.None);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(_channel, TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task OnNewEntryJoiningFollowedLeague_PostsToSubscribedChannel()
    {
        var action = BuildAction(LeagueWith(John()), LeagueWith(John(), Jane()));

        await action.Process(CancellationToken.None);
        await action.Process(CancellationToken.None);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(_channel);
        Assert.Contains("Jane Doe", msg.Text);
        Assert.Contains("YOLO league", msg.Text);
        Assert.DoesNotContain("John Korsnes", msg.Text);
    }

    [Fact]
    public async Task OnNoNewEntriesSinceLastPoll_PostsNothing()
    {
        var action = BuildAction(LeagueWith(John()), LeagueWith(John()));

        await action.Process(CancellationToken.None);
        await action.Process(CancellationToken.None);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(_channel, TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task OnEntryStillListedAsNewOnNextPoll_PostsOnlyOnce()
    {
        var action = BuildAction(LeagueWith(John()), LeagueWith(John(), Jane()), LeagueWith(John(), Jane()));

        await action.Process(CancellationToken.None);
        await action.Process(CancellationToken.None);
        await action.Process(CancellationToken.None);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(_channel);
        Assert.Contains("Jane Doe", msg.Text);
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(_channel, TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task OnEntryJoiningLeagueWithoutPendingEntries_PostsToSubscribedChannel()
    {
        // Nothing is pending when the league is first polled, so tracking starts at poll time —
        // an entry joining after that (hence the future join time) is the one to notify about.
        var joinedAfterFirstPoll = Jane();
        joinedAfterFirstPoll.JoinedAt = DateTime.UtcNow.AddMinutes(1);
        var action = BuildAction(LeagueWith(), LeagueWith(joinedAfterFirstPoll));

        await action.Process(CancellationToken.None);
        await action.Process(CancellationToken.None);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(_channel);
        Assert.Contains("Jane Doe", msg.Text);
    }

    [Fact]
    public async Task OnNewEntryJoiningAnotherLeague_PostsNothingToChannelFollowingOtherLeague()
    {
        var otherChannel = "#other-" + Guid.NewGuid().ToString("N")[..8];
        await fixture.Subscribe(_teamId, otherChannel, FplEvent.NewLeagueEntries);
        var repo = fixture.Services.GetRequiredService<ISlackTeamRepository>();
        var installation = await repo.GetInstallation(_teamId);
        installation.Follow(otherChannel, new ClassicLeagueId(_leagueId + 1));
        await repo.Save(installation);

        var action = BuildAction(LeagueWith(John()), LeagueWith(John(), Jane()));
        await action.Process(CancellationToken.None);
        await action.Process(CancellationToken.None);

        await fixture.SlackCapture.WaitForMessageAsync(_channel);
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(otherChannel, TimeSpan.FromMilliseconds(500)));
    }

    private static NewLeagueEntry John() => new()
    {
        Entry = 700843,
        EntryName = "Takk for meg",
        PlayerFirstName = "John",
        PlayerLastName = "Korsnes",
        JoinedAt = FirstJoin
    };

    private static NewLeagueEntry Jane() => new()
    {
        Entry = 800123,
        EntryName = "Jane's XI",
        PlayerFirstName = "Jane",
        PlayerLastName = "Doe",
        JoinedAt = SecondJoin
    };

    private ClassicLeague LeagueWith(params NewLeagueEntry[] newEntries) => new()
    {
        Properties = new ClassicLeagueProperties { Id = _leagueId, Name = "YOLO league", LeagueType = "x" },
        NewEntries = new NewLeagueEntries { Entries = newEntries }
    };

    private NewLeagueEntriesRecurringAction BuildAction(params ClassicLeague[] pollResults)
    {
        var leagueClient = fixture.Services.GetRequiredService<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(_leagueId, A<int>._, A<bool>._)).ReturnsNextFromSequence(pollResults);

        return new NewLeagueEntriesRecurringAction(
            leagueClient,
            fixture.Services.GetRequiredService<ISlackTeamRepository>(),
            fixture.Services.GetRequiredService<IGuildRepository>(),
            new LeagueEntriesRedisBookmarkProvider(fixture.Services.GetRequiredService<IConnectionMultiplexer>(),
                A.Fake<ILogger<LeagueEntriesRedisBookmarkProvider>>()),
            fixture.Services.GetRequiredService<IServiceScopeFactory>(),
            A.Fake<ILogger<NewLeagueEntriesRecurringAction>>());
    }
}
