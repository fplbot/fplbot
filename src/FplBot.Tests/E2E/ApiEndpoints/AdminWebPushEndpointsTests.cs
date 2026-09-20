using System.Net;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.PulseLive;
using FplBot.WebApi.Endpoints.Api.Admin;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.E2E.ApiEndpoints;

[Collection("App")]
public class AdminWebPushEndpointsTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.WebPushCapture.Reset();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task List_ReturnsSubscribersWithEndpointHostOnly()
    {
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: "https://push.example.test/abc", name: "iPhone");

        var response = await fixture.Get("/api/admin/web/subscribers?page=0&pageSize=20");
        response.EnsureSuccessStatusCode();
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var page = await AppFixture.ReadJson<PagedResult<SubscriberSummaryDto>>(response);

        var item = Assert.Single(page.Items);
        Assert.Equal("iPhone", item.Name);
        Assert.Equal(123, item.LeagueId);
        Assert.Equal("push.example.test", item.EndpointHost);
        Assert.DoesNotContain("/abc", raw);
        Assert.DoesNotContain("BFakeP256dhKeyForTests", raw);
        Assert.DoesNotContain("FakeAuthSecret", raw);
    }

    [Fact]
    public async Task List_PagesThroughSubscribers()
    {
        await fixture.SubscribeToWebPush();
        await fixture.SubscribeToWebPush();
        await fixture.SubscribeToWebPush();

        var firstPage = await fixture.GetJson<PagedResult<SubscriberSummaryDto>>("/api/admin/web/subscribers?page=0&pageSize=2");
        var secondPage = await fixture.GetJson<PagedResult<SubscriberSummaryDto>>("/api/admin/web/subscribers?page=1&pageSize=2");

        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Single(secondPage.Items);
        Assert.DoesNotContain(secondPage.Items, s => firstPage.Items.Any(f => f.Id == s.Id));
    }

    [Fact]
    public async Task GetSingle_ReturnsTheSubscriber()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 456, name: "Pixel");

        var subscriber = await fixture.GetJson<SubscriberDetailDto>($"/api/admin/web/subscribers/{subscriberId}");

        Assert.Equal(subscriberId, subscriber.Id);
        Assert.Equal("Pixel", subscriber.Name);
        Assert.Equal(456, subscriber.LeagueId);
        Assert.Contains("Standings", subscriber.Events);
        Assert.Contains("FixtureGoals", subscriber.Available);
        Assert.Contains("Standings", subscriber.RequiresLeague);
    }

    [Fact]
    public async Task GetSingle_UnknownSubscriber_ReturnsNotFound()
    {
        var response = await fixture.Get("/api/admin/web/subscribers/nope");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutEvents_AdminCanChangeASubscribersEvents()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123);

        var response = await fixture.Put($"/api/admin/web/subscribers/{subscriberId}/events", new { events = new[] { "Deadlines" } });
        response.EnsureSuccessStatusCode();
        var subscriber = await AppFixture.ReadJson<SubscriberDetailDto>(response);

        Assert.Equal(["Deadlines"], subscriber.Events);
    }

    [Fact]
    public async Task PutEvents_UnknownSubscriber_ReturnsNotFound()
    {
        var response = await fixture.Put("/api/admin/web/subscribers/nope/events", new { events = new[] { "Deadlines" } });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutLeague_AdminCanSetALeague()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: null);

        var response = await fixture.Put($"/api/admin/web/subscribers/{subscriberId}/league", new { leagueId = 789 });
        response.EnsureSuccessStatusCode();
        var subscriber = await AppFixture.ReadJson<SubscriberDetailDto>(response);

        Assert.Equal(789, subscriber.LeagueId);
        Assert.DoesNotContain("Standings", subscriber.Events);

        var afterEnablingStandings = await fixture.Put($"/api/admin/web/subscribers/{subscriberId}/events",
            new { events = subscriber.Events.Append("Standings") });
        afterEnablingStandings.EnsureSuccessStatusCode();
        var updated = await AppFixture.ReadJson<SubscriberDetailDto>(afterEnablingStandings);

        Assert.Contains("Standings", updated.Events);
    }

    [Fact]
    public async Task PutLeague_Null_UnfollowsAndDropsLeagueRequiringEvents()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123);

        var response = await fixture.Put($"/api/admin/web/subscribers/{subscriberId}/league", new { leagueId = (long?)null });
        response.EnsureSuccessStatusCode();
        var subscriber = await AppFixture.ReadJson<SubscriberDetailDto>(response);

        Assert.Null(subscriber.LeagueId);
        Assert.DoesNotContain("Standings", subscriber.Events);
    }

    [Fact]
    public async Task PutLeague_InvalidLeagueId_ReturnsBadRequest()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123);

        var response = await fixture.Put($"/api/admin/web/subscribers/{subscriberId}/league", new { leagueId = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTheSubscriber()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123);

        var deleted = await fixture.Delete($"/api/admin/web/subscribers/{subscriberId}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var page = await fixture.GetJson<PagedResult<SubscriberSummaryDto>>("/api/admin/web/subscribers?page=0&pageSize=20");
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task Publish_Standings_SubscriberFollowingLeague_PublishesNow()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        var response = await fixture.Post($"/api/admin/web/subscribers/{subscriberId}/publish/Standings");
        response.EnsureSuccessStatusCode();

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("standings", push.Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Publish_Standings_SubscriberNotFollowingLeague_DoesNotPublish()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: null);

        var response = await fixture.Post($"/api/admin/web/subscribers/{subscriberId}/publish/Standings");
        var value = await AppFixture.ReadJson<System.Text.Json.JsonElement>(response);

        Assert.False(value.GetProperty("published").GetBoolean());
        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.WebPushCapture.Any());
    }

    [Fact]
    public async Task Publish_Deadline24Hours_SubscriberNotFollowingLeague_StillPublishes()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: null, endpoint: endpoint);

        var response = await fixture.Post($"/api/admin/web/subscribers/{subscriberId}/publish/Deadline24Hours");
        response.EnsureSuccessStatusCode();

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("24 hours", push.Body);
    }

    [Fact]
    public async Task Publish_Deadline1Hour_SubscriberNotFollowingLeague_StillPublishes()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: null, endpoint: endpoint);

        var response = await fixture.Post($"/api/admin/web/subscribers/{subscriberId}/publish/Deadline1Hour");
        response.EnsureSuccessStatusCode();

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("60 minutes", push.Body);
    }

    // The current gameweek in bootstrap-static.json (see GameweekExtensions.GetCurrentGameweek).
    private const int CurrentGameweekId = 3;

    private static Fixture RealPlayedFixture(bool finished = false) => new()
    {
        Id = 1,
        Code = 1,
        Event = CurrentGameweekId,
        HomeTeamId = 1,
        AwayTeamId = 2,
        KickOffTime = DateTime.UtcNow.AddHours(-2),
        Minutes = 90,
        Finished = finished,
        FinishedProvisional = finished,
        HomeTeamScore = finished ? 2 : 1,
        AwayTeamScore = finished ? 1 : 0,
        Stats =
        [
            new FixtureStat
            {
                Identifier = "goals_scored",
                HomeStats = [new FixtureStatValue { Element = 1, Value = finished ? 2 : 1 }],
                AwayStats = finished ? [new FixtureStatValue { Element = 2, Value = 1 }] : []
            }
        ]
    };

    private void SeedFixture(Fixture fixture_) =>
        A.CallTo(() => fixture.Services.GetRequiredService<IFixtureClient>().GetFixturesByGameweek(CurrentGameweekId))
            .Returns([fixture_]);

    private void SeedLineups()
    {
        var homeLineup = new TeamLineup
        {
            TeamId = 1,
            Players = [new PulsePlayer { Id = 1, KnownName = "Raya", Position = "Goalkeeper", IsCaptain = false }],
            Formation = new PulseFormation { Label = "4-3-3", Lineup = [[1]] }
        };
        var awayLineup = new TeamLineup
        {
            TeamId = 2,
            Players = [new PulsePlayer { Id = 2, KnownName = "Martinez", Position = "Goalkeeper", IsCaptain = false }],
            Formation = new PulseFormation { Label = "4-4-2", Lineup = [[2]] }
        };
        A.CallTo(() => fixture.Services.GetRequiredService<IPulseLiveClient>().GetMatchDetails(1))
            .Returns(new MatchDetails { HomeTeam = homeLineup, AwayTeam = awayLineup });
    }

    [Fact]
    public async Task Publish_FixtureEvents_NoFixturesForGameweek_DoesNotPublish()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: null, endpoint: endpoint);
        A.CallTo(() => fixture.Services.GetRequiredService<IFixtureClient>().GetFixturesByGameweek(CurrentGameweekId)).Returns([]);

        var response = await fixture.Post($"/api/admin/web/subscribers/{subscriberId}/publish/FixtureEvents");

        var value = await AppFixture.ReadJson<System.Text.Json.JsonElement>(response);
        Assert.False(value.GetProperty("published").GetBoolean());
        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.WebPushCapture.Any());
    }

    [Fact]
    public async Task Publish_FixtureEvents_RealGoalRecorded_PublishesItWithNoLink()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        // Following a league would give a Standings/GameweekStarted push a /leagues/{id} link, but a
        // fixture event has no league-specific destination, so it must not carry one either.
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);
        SeedFixture(RealPlayedFixture());

        var response = await fixture.Post($"/api/admin/web/subscribers/{subscriberId}/publish/FixtureEvents");
        response.EnsureSuccessStatusCode();

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("Raya", push.Body);
        Assert.Null(push.Link);
    }

    [Fact]
    public async Task Publish_FixtureFullTime_FixtureFinished_PublishesScoreWithNoLink()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);
        SeedFixture(RealPlayedFixture(finished: true));

        var response = await fixture.Post($"/api/admin/web/subscribers/{subscriberId}/publish/FixtureFullTime");
        response.EnsureSuccessStatusCode();

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("2-1", push.Title);
        Assert.Null(push.Link);
    }

    [Fact]
    public async Task Publish_Lineups_Confirmed_PublishesFormattedLineupWithNoLink()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);
        SeedFixture(RealPlayedFixture());
        SeedLineups();

        var response = await fixture.Post($"/api/admin/web/subscribers/{subscriberId}/publish/Lineups");
        response.EnsureSuccessStatusCode();

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Contains("Lineups", push.Title);
        Assert.Contains("Raya", push.Body);
        Assert.Null(push.Link);
    }

    [Fact]
    public async Task Publish_UnknownEventName_ReturnsBadRequest()
    {
        var subscriberId = await fixture.SubscribeToWebPush(leagueId: 123);

        var response = await fixture.Post($"/api/admin/web/subscribers/{subscriberId}/publish/NotARealEvent");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Publish_UnknownSubscriber_ReturnsNotFound()
    {
        var response = await fixture.Post("/api/admin/web/subscribers/nope/publish/Standings");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Broadcast_SendsToSubscribers()
    {
        var endpoint = $"https://push.example.test/{Guid.NewGuid():N}";
        await fixture.SubscribeToWebPush(leagueId: 123, endpoint: endpoint);

        var response = await fixture.Post("/api/admin/web/broadcast", new { title = "Heads up", body = "FplBot has news" });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var push = await fixture.WebPushCapture.WaitForAsync(endpoint);
        Assert.Equal("Heads up", push.Title);
        Assert.Equal("FplBot has news", push.Body);
    }
}
