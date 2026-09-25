using FplBot.Data.Web;
using FplBot.Domain;
using FplBot.Integrations.WebPush;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Microsoft.Extensions.Options;

namespace FplBot.WebApi.Endpoints.Api.Web;

public record SubscribeRequest(string Endpoint, string P256dh, string Auth, long? LeagueId, string? Name, long? EntryId = null);

public record SubscribeResponse(string SubscriberId);

public record SubscriberStateResponse(string SubscriberId, long? LeagueId, long? EntryId, string[] Events, string[] Available, string[] RequiresLeague);

public record EventsRequest(string[] Events);

public record LeagueRequest(long? LeagueId);

public record EntryRequest(long? EntryId);

public record VapidKeyResponse(string PublicKey);

public static class WebPushEndpoints
{
    private const string SubscriberIdHeader = "X-Subscriber-Id";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/push/key", GetKey);
        group.MapPost("/push/subscribe", Subscribe);
        group.MapGet("/me", GetMe);
        group.MapPut("/me/events", PutEvents);
        group.MapPut("/me/league", PutLeague);
        group.MapPut("/me/entry", PutEntry);
        group.MapDelete("/me", DeleteMe);
        group.MapPost("/me/test", PostTest);
    }

    internal static IResult GetKey(IOptions<WebPushOptions> options) =>
        TypedResults.Ok(new VapidKeyResponse(options.Value.PublicKey));

    internal static async Task<IResult> Subscribe(SubscribeRequest request, IWebPushSubscriberRepository repo)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint) || string.IsNullOrWhiteSpace(request.P256dh) ||
            string.IsNullOrWhiteSpace(request.Auth))
        {
            return TypedResults.BadRequest(new { errors = new { push = new[] { "endpoint, p256dh and auth are required" } } });
        }

        if (!Uri.TryCreate(request.Endpoint, UriKind.Absolute, out var endpointUri) ||
            endpointUri.Scheme != Uri.UriSchemeHttps)
        {
            return TypedResults.BadRequest(new { errors = new { push = new[] { "endpoint must be an absolute https URL" } } });
        }

        if (request.LeagueId is { } leagueId && !IsValidLeagueId(leagueId))
        {
            return TypedResults.BadRequest(new { errors = new { leagueId = new[] { "leagueId must be between 1 and 2147483647" } } });
        }

        if (request.EntryId is { } entryId && !IsValidEntryId(entryId))
        {
            return TypedResults.BadRequest(new { errors = new { entryId = new[] { "entryId must be between 1 and 2147483647" } } });
        }

        var subscriber = WebPushSubscriber.Register(
            new PushKeys(request.Endpoint, request.P256dh, request.Auth), request.Name,
            request.LeagueId is { } league ? new ClassicLeagueId(league) : null,
            request.EntryId is { } entry ? new FplEntryId(entry) : null);

        await repo.Save(subscriber);
        return TypedResults.Ok(new SubscribeResponse(subscriber.Id.Value));
    }

    internal static async Task<IResult> GetMe(HttpContext context, IWebPushSubscriberRepository repo) =>
        await Resolve(context, repo) is { } subscriber ? TypedResults.Ok(ToState(subscriber)) : TypedResults.NotFound();

    internal static async Task<IResult> PutEvents(HttpContext context, EventsRequest request, IWebPushSubscriberRepository repo)
    {
        if (await Resolve(context, repo) is not { } subscriber)
        {
            return TypedResults.NotFound();
        }

        subscriber.Unsubscribe([..subscriber.Events.Current]);
        subscriber.Subscribe(Parse(request.Events));
        await repo.Save(subscriber);
        return TypedResults.Ok(ToState(subscriber));
    }

    internal static async Task<IResult> PutLeague(HttpContext context, LeagueRequest request, IWebPushSubscriberRepository repo)
    {
        if (await Resolve(context, repo) is not { } subscriber)
        {
            return TypedResults.NotFound();
        }

        if (request.LeagueId is { } invalid && !IsValidLeagueId(invalid))
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
        return TypedResults.Ok(ToState(subscriber));
    }

    internal static async Task<IResult> PutEntry(HttpContext context, EntryRequest request, IWebPushSubscriberRepository repo)
    {
        if (await Resolve(context, repo) is not { } subscriber)
        {
            return TypedResults.NotFound();
        }

        if (request.EntryId is { } invalid && !IsValidEntryId(invalid))
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
        return TypedResults.Ok(ToState(subscriber));
    }

    internal static async Task<IResult> DeleteMe(HttpContext context, IWebPushSubscriberRepository repo)
    {
        if (await Resolve(context, repo) is not { } subscriber)
        {
            return TypedResults.NotFound();
        }

        await repo.Delete(subscriber.Id);
        return TypedResults.NoContent();
    }

    internal static async Task<IResult> PostTest(HttpContext context, IWebPushSubscriberRepository repo,
        IPublishEndpoint publishEndpoint)
    {
        if (await Resolve(context, repo) is not { } subscriber)
        {
            return TypedResults.NotFound();
        }

        await publishEndpoint.Publish(new PublishToWebPushSubscriber(subscriber.Id.Value,
            "FplBot", "Notifications are working.", (int?)subscriber.FollowedLeagueId?.Value));

        return TypedResults.Accepted($"/api/web/me");
    }

    internal static async Task<WebPushSubscriber?> Resolve(HttpContext context, IWebPushSubscriberRepository repo) =>
        context.Request.Headers.TryGetValue(SubscriberIdHeader, out var header) && !string.IsNullOrWhiteSpace(header)
            ? await repo.Find(new WebPushSubscriberId(header.ToString()))
            : null;

    internal static bool IsValidLeagueId(long leagueId) => leagueId is >= 1 and <= int.MaxValue;

    internal static bool IsValidEntryId(long entryId) => entryId is >= 1 and <= int.MaxValue;

    internal static FplEvent[] Parse(IEnumerable<string> events) =>
        [
            ..events.Select(e => Enum.TryParse<FplEvent>(e, out var parsed) ? parsed : (FplEvent?)null)
                .Where(e => e.HasValue)
                .Select(e => e!.Value)
        ];

    private static SubscriberStateResponse ToState(WebPushSubscriber subscriber) =>
        new(subscriber.Id.Value,
            subscriber.FollowedLeagueId?.Value,
            subscriber.LinkedEntryId?.Value,
            [..subscriber.Events.Current.Select(e => e.ToString())],
            [..FplEvents.SupportedOnWeb.Select(e => e.ToString())],
            [..FplEvents.RequiringALeague.Select(e => e.ToString())]);
}
