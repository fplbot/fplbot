using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Models.Mappers;
using Fpl.PulseLive;
using FplBot.Data.Web;
using FplBot.Domain;
using FplBot.EventHandlers.Web;
using FplBot.Formatting;
using FplBot.Formatting.FixtureStats;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.WebApi.Endpoints.Api.Web;
using MassTransit;

namespace FplBot.WebApi.Endpoints.Api.Admin;

public record SubscriberSummaryDto(string Id, string? Name, long? LeagueId, long? EntryId, int EventCount, string EndpointHost);

public record SubscriberDetailDto(string Id, string? Name, long? LeagueId, long? EntryId, string[] Events, string[] Available,
    string[] RequiresLeague, string EndpointHost);

public record WebPushBroadcastRequest(string Title, string Body);

public static class AdminWebPushEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/web/subscribers", GetSubscribers);
        group.MapGet("/web/subscribers/{subscriberId}", GetSubscriber);
        group.MapPut("/web/subscribers/{subscriberId}/events", PutEvents);
        group.MapPut("/web/subscribers/{subscriberId}/league", PutLeague);
        group.MapPut("/web/subscribers/{subscriberId}/entry", PutEntry);
        group.MapDelete("/web/subscribers/{subscriberId}", DeleteSubscriber);
        group.MapPost("/web/subscribers/{subscriberId}/publish/{eventName}", Publish);
        group.MapPost("/web/broadcast", Broadcast);
    }

    internal static async Task<IResult> GetSubscribers(IWebPushSubscriberRepository repo, int page = 0, int pageSize = 20)
    {
        var (items, total) = await repo.GetPage(page, pageSize);
        return TypedResults.Ok(new PagedResult<SubscriberSummaryDto>(
            [.. items.Select(ToSummaryDto)], page, pageSize, total));
    }

    internal static async Task<IResult> GetSubscriber(string subscriberId, IWebPushSubscriberRepository repo) =>
        await repo.Find(new WebPushSubscriberId(subscriberId)) is { } subscriber
            ? TypedResults.Ok(ToDetailDto(subscriber))
            : TypedResults.NotFound();

    internal static async Task<IResult> PutEvents(string subscriberId, EventsRequest request, IWebPushSubscriberRepository repo)
    {
        if (await repo.Find(new WebPushSubscriberId(subscriberId)) is not { } subscriber)
        {
            return TypedResults.NotFound();
        }

        subscriber.Unsubscribe([.. subscriber.Events.Current]);
        subscriber.Subscribe(WebPushEndpoints.Parse(request.Events));
        await repo.Save(subscriber);
        return TypedResults.Ok(ToDetailDto(subscriber));
    }

    internal static async Task<IResult> PutLeague(string subscriberId, LeagueRequest request, IWebPushSubscriberRepository repo)
    {
        if (await repo.Find(new WebPushSubscriberId(subscriberId)) is not { } subscriber)
        {
            return TypedResults.NotFound();
        }

        if (request.LeagueId is { } invalid && !WebPushEndpoints.IsValidLeagueId(invalid))
        {
            return TypedResults.BadRequest(new { errors = new { leagueId = new[] { "leagueId must be between 1 and 2147483647" } } });
        }

        if (request.LeagueId is { } leagueId)
        {
            subscriber.Follow(new ClassicLeagueId(leagueId));
        }
        else
        {
            subscriber.Unfollow();
        }

        await repo.Save(subscriber);
        return TypedResults.Ok(ToDetailDto(subscriber));
    }

    internal static async Task<IResult> PutEntry(string subscriberId, EntryRequest request, IWebPushSubscriberRepository repo)
    {
        if (await repo.Find(new WebPushSubscriberId(subscriberId)) is not { } subscriber)
        {
            return TypedResults.NotFound();
        }

        if (request.EntryId is { } invalid && !WebPushEndpoints.IsValidEntryId(invalid))
        {
            return TypedResults.BadRequest(new { errors = new { entryId = new[] { "entryId must be between 1 and 2147483647" } } });
        }

        if (request.EntryId is { } entryId)
        {
            subscriber.LinkEntry(new FplEntryId(entryId));
        }
        else
        {
            subscriber.UnlinkEntry();
        }

        await repo.Save(subscriber);
        return TypedResults.Ok(ToDetailDto(subscriber));
    }

    internal static async Task<IResult> DeleteSubscriber(string subscriberId, IWebPushSubscriberRepository repo)
    {
        await repo.Delete(new WebPushSubscriberId(subscriberId));
        return TypedResults.NoContent();
    }

    internal static async Task<IResult> Publish(string subscriberId, string eventName, IWebPushSubscriberRepository repo,
        IPublishEndpoint publishEndpoint, IGlobalSettingsClient gameweekClient, IFixtureClient fixtureClient,
        IPulseLiveClient pulseClient, ILiveClient liveClient)
    {
        if (!Enum.TryParse<PublishableEvent>(eventName, ignoreCase: true, out var evt))
        {
            return TypedResults.BadRequest(new { errors = new { eventName = new[] { "must be one of Standings, GameweekStarted, Deadline24Hours, Deadline1Hour, FixtureEvents, FixtureFullTime, Lineups" } } });
        }

        if (await repo.Find(new WebPushSubscriberId(subscriberId)) is not { } subscriber)
        {
            return TypedResults.NotFound();
        }

        var requiresLeague = evt is PublishableEvent.Standings or PublishableEvent.GameweekStarted;
        if (requiresLeague && subscriber.FollowedLeagueId is null)
        {
            return TypedResults.Ok(new { published = false, message = "Did not publish. Subscriber is not following a league." });
        }

        var settings = await gameweekClient.GetGlobalSettings();
        if (settings?.Gameweeks.GetCurrentGameweek() is not { } gameweek)
        {
            return TypedResults.Ok(new { published = false, message = "Could not determine the current gameweek." });
        }

        switch (evt)
        {
            case PublishableEvent.Standings:
                await publishEndpoint.Publish(new ProcessGameweekFinishedForWebPushSubscriber(subscriber.Id.Value, (int)subscriber.FollowedLeagueId!.Value, gameweek.Id));
                return TypedResults.Ok(new { published = true, message = "Published." });
            case PublishableEvent.GameweekStarted:
                await publishEndpoint.Publish(new ProcessGameweekStartedForWebPushSubscriber(subscriber.Id.Value, (int)subscriber.FollowedLeagueId!.Value, gameweek.Id));
                return TypedResults.Ok(new { published = true, message = "Published." });
            case PublishableEvent.FixtureEvents:
            {
                if (await PublishableFixtureLookup.FindFirstFixture(fixtureClient, gameweek.Id) is not { } fixture)
                {
                    return TypedResults.Ok(new { published = false, message = "No fixtures found for the current gameweek." });
                }

                var (events, refusalReason) = await PublishableFixtureLookup.ResolveFixtureEvents(fixture, gameweekClient);
                if (refusalReason is not null)
                {
                    return TypedResults.Ok(new { published = false, message = refusalReason });
                }

                var messages = GameweekEventsFormatter.FormatNewFixtureEvents(events, _ => true, FormattingType.Web);
                foreach (var message in messages)
                {
                    await publishEndpoint.Publish(new PublishToWebPushSubscriber(subscriber.Id.Value, message.Title, message.Details, null));
                }

                return TypedResults.Ok(new
                {
                    published = messages.Count > 0,
                    message = messages.Count > 0 ? $"Published {messages.Count} fixture event notification(s)." : "No publishable player events found."
                });
            }
            case PublishableEvent.FixtureFullTime:
            {
                if (await PublishableFixtureLookup.FindFirstFixture(fixtureClient, gameweek.Id) is not { } fixture)
                {
                    return TypedResults.Ok(new { published = false, message = "No fixtures found for the current gameweek." });
                }

                if (!fixture.Finished && !fixture.FinishedProvisional)
                {
                    return TypedResults.Ok(new { published = false, message = "This fixture hasn't finished yet." });
                }

                var fulltimeSettings = await gameweekClient.GetGlobalSettings();
                var liveItems = fixture.Event.HasValue ? await liveClient.GetLiveItems(fixture.Event.Value, isOngoingGameweek: true) : null;
                var finished = FixtureFulltimeModelBuilder.CreateFinishedFixture(fulltimeSettings?.Teams ?? [], fulltimeSettings?.Players ?? [], fixture, liveItems);
                var (ftTitle, ftBody) = WebPushMessages.FixtureFullTime(finished);
                await publishEndpoint.Publish(new PublishToWebPushSubscriber(subscriber.Id.Value, ftTitle, ftBody, null));
                return TypedResults.Ok(new { published = true, message = "Published." });
            }
            case PublishableEvent.Lineups:
            {
                if (await PublishableFixtureLookup.FindFirstFixture(fixtureClient, gameweek.Id) is not { } fixture)
                {
                    return TypedResults.Ok(new { published = false, message = "No fixtures found for the current gameweek." });
                }

                var matchDetails = await pulseClient.GetMatchDetails(fixture.Code);
                if (matchDetails is null || !matchDetails.HasLineUps())
                {
                    return TypedResults.Ok(new { published = false, message = "Lineups aren't confirmed yet for this fixture." });
                }

                var lineupSettings = await gameweekClient.GetGlobalSettings();
                var teamShortNames = lineupSettings?.Teams.ToDictionary(t => t.Id, t => t.ShortName) ?? [];
                var homeAbbr = teamShortNames.GetValueOrDefault(fixture.HomeTeamId, "?") ?? "?";
                var awayAbbr = teamShortNames.GetValueOrDefault(fixture.AwayTeamId, "?") ?? "?";
                var lineupReady = MatchDetailsMapper.TryMapToLineup(matchDetails, fixture.Code, homeAbbr, awayAbbr);
                if (lineupReady is null)
                {
                    return TypedResults.Ok(new { published = false, message = "Could not map lineups for this fixture." });
                }

                var (luTitle, luBody) = WebPushMessages.Lineups(lineupReady.Lineup);
                await publishEndpoint.Publish(new PublishToWebPushSubscriber(subscriber.Id.Value, luTitle, luBody, null));
                return TypedResults.Ok(new { published = true, message = "Published." });
            }
        }

        // The real system never sends an arbitrary "time remaining" reminder — only ever one of
        // these two fixed messages, so "publish now" replays one verbatim rather than computing
        // a countdown that would say something the real system never actually says.
        var (title, body) = evt switch
        {
            PublishableEvent.Deadline24Hours => WebPushMessages.DeadlineReminder(gameweek.Id, "in 24 hours"),
            PublishableEvent.Deadline1Hour => WebPushMessages.DeadlineReminder(gameweek.Id, "in 60 minutes"),
            _ => throw new ArgumentOutOfRangeException(nameof(evt))
        };

        await publishEndpoint.Publish(new PublishToWebPushSubscriber(subscriber.Id.Value, title, body, (int?)subscriber.FollowedLeagueId?.Value));

        return TypedResults.Ok(new { published = true, message = $"Published {evt} to subscriber" });
    }

    internal static async Task<IResult> Broadcast(WebPushBroadcastRequest request, IPublishEndpoint publishEndpoint)
    {
        await publishEndpoint.Publish(new BroadcastToWebPush(request.Title, request.Body));
        return TypedResults.Accepted("/api/admin/web/subscribers");
    }

    private static SubscriberSummaryDto ToSummaryDto(WebPushSubscriber subscriber) =>
        new(subscriber.Id.Value,
            subscriber.Name,
            subscriber.FollowedLeagueId?.Value,
            subscriber.LinkedEntryId?.Value,
            subscriber.Events.Current.Count,
            EndpointHost(subscriber));

    private static SubscriberDetailDto ToDetailDto(WebPushSubscriber subscriber) =>
        new(subscriber.Id.Value,
            subscriber.Name,
            subscriber.FollowedLeagueId?.Value,
            subscriber.LinkedEntryId?.Value,
            [.. subscriber.Events.Current.Select(e => e.ToString())],
            [.. FplEvents.SupportedOnWeb.Select(e => e.ToString())],
            [.. FplEvents.RequiringALeague.Select(e => e.ToString())],
            EndpointHost(subscriber));

    private static string EndpointHost(WebPushSubscriber subscriber) =>
        Uri.TryCreate(subscriber.PushKeys.Endpoint, UriKind.Absolute, out var uri) ? uri.Host : "unknown";
}
