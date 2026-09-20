using FplBot.Data.Web;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.WebApi.Endpoints.Api.Admin;

public record SubscriberSummaryDto(string Id, string? Name, long? LeagueId, int EventCount, string EndpointHost);

public record WebPushBroadcastRequest(string Title, string Body);

public static class AdminWebPushEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/web/subscribers", GetSubscribers);
        group.MapGet("/web/subscribers/{subscriberId}", GetSubscriber);
        group.MapDelete("/web/subscribers/{subscriberId}", DeleteSubscriber);
        group.MapPost("/web/broadcast", Broadcast);
    }

    internal static async Task<IResult> GetSubscribers(IWebPushSubscriberRepository repo, int page = 0, int pageSize = 20)
    {
        var (items, total) = await repo.GetPage(page, pageSize);
        return TypedResults.Ok(new PagedResult<SubscriberSummaryDto>(
            [.. items.Select(ToDto)], page, pageSize, total));
    }

    internal static async Task<IResult> GetSubscriber(string subscriberId, IWebPushSubscriberRepository repo) =>
        await repo.Find(new WebPushSubscriberId(subscriberId)) is { } subscriber
            ? TypedResults.Ok(ToDto(subscriber))
            : TypedResults.NotFound();

    internal static async Task<IResult> DeleteSubscriber(string subscriberId, IWebPushSubscriberRepository repo)
    {
        await repo.Delete(new WebPushSubscriberId(subscriberId));
        return TypedResults.NoContent();
    }

    internal static async Task<IResult> Broadcast(WebPushBroadcastRequest request, IPublishEndpoint publishEndpoint)
    {
        await publishEndpoint.Publish(new BroadcastToWebPush(request.Title, request.Body));
        return TypedResults.Accepted("/api/admin/web/subscribers");
    }

    private static SubscriberSummaryDto ToDto(WebPushSubscriber subscriber) =>
        new(subscriber.Id.Value,
            subscriber.Name,
            subscriber.FollowedLeagueId?.Value,
            subscriber.Events.Current.Count,
            new Uri(subscriber.PushKeys.Endpoint).Host);
}
