using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.RecurringActions;
using FplBot.Data;
using FplBot.Data.Discord;
using FplBot.Data.Leagues;
using FplBot.Data.Slack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FplBot.Tests.E2E.Discord;

[Collection("App")]
public class DiscordNewLeagueEntriesHandlerTests(AppFixture fixture) : IAsyncLifetime
{
    private const int LeagueId = 31936;
    private static readonly DateTime FirstJoin = new(2021, 11, 30, 11, 25, 6, DateTimeKind.Utc);
    private static readonly DateTime SecondJoin = new(2021, 12, 1, 9, 0, 0, DateTimeKind.Utc);

    public async ValueTask InitializeAsync()
    {
        fixture.DiscordCapture.Reset();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task OnNewEntryJoiningFollowedLeague_PostsToSubscribedChannel()
    {
        var installedGuild = await fixture.SeedGuildInstallation(LeagueId, [EventSubscription.NewLeagueEntries]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;
        var action = BuildAction(LeagueWith(John()), LeagueWith(John(), Jane()));

        await action.Process(CancellationToken.None);
        await action.Process(CancellationToken.None);

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("Jane Doe", msg.Description);
        Assert.Contains("YOLO league", msg.Description);
    }

    [Fact]
    public async Task OnNewEntryJoiningFollowedLeague_ChannelNotSubscribed_PostsNothing()
    {
        await fixture.SeedGuildInstallation(LeagueId, [EventSubscription.PriceChanges]);
        var action = BuildAction(LeagueWith(John()), LeagueWith(John(), Jane()));

        await action.Process(CancellationToken.None);
        await action.Process(CancellationToken.None);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.DiscordCapture.WaitForMessageAsync(TimeSpan.FromMilliseconds(500)));
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

    private static ClassicLeague LeagueWith(params NewLeagueEntry[] newEntries) => new()
    {
        Properties = new ClassicLeagueProperties { Id = LeagueId, Name = "YOLO league", LeagueType = "x" },
        NewEntries = new NewLeagueEntries { Entries = newEntries }
    };

    private NewLeagueEntriesRecurringAction BuildAction(params ClassicLeague[] pollResults)
    {
        var leagueClient = fixture.Services.GetRequiredService<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(LeagueId, A<int>._, A<bool>._)).ReturnsNextFromSequence(pollResults);

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
