using FplBot.WebApi.Admin;

namespace FplBot.WebApi.Endpoints.Api.Admin;

public record PurgeResult(int Purged);

public static class AdminErrorEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/errors/queues", GetQueues);
        group.MapGet("/errors/queue/messages", GetMessages);
        group.MapPost("/errors/queue/messages/{messageId}/retry", RetryMessage);
        group.MapPost("/errors/queue/messages/{messageId}/discard", DiscardMessage);
        group.MapPost("/errors/queue/purge", PurgeQueue);
    }

    internal static async Task<IResult> GetQueues(AdminErrorQueueService service, CancellationToken ct)
        => TypedResults.Ok(await service.ListQueuesAsync(ct));

    internal static async Task<IResult> GetMessages(string topic, string subscription, AdminErrorQueueService service, CancellationToken ct)
        => TypedResults.Ok(await service.PeekMessagesAsync(topic, subscription, ct: ct));

    internal static async Task<IResult> RetryMessage(string messageId, string topic, string subscription, AdminErrorQueueService service, CancellationToken ct)
    {
        var retried = await service.RetryMessageAsync(topic, subscription, messageId, ct);
        return retried ? TypedResults.Ok(new { message = "Message retried" }) : TypedResults.NotFound();
    }

    internal static async Task<IResult> DiscardMessage(string messageId, string topic, string subscription, AdminErrorQueueService service, CancellationToken ct)
    {
        var discarded = await service.DiscardMessageAsync(topic, subscription, messageId, ct);
        return discarded ? TypedResults.Ok(new { message = "Message discarded" }) : TypedResults.NotFound();
    }

    internal static async Task<IResult> PurgeQueue(string topic, string subscription, AdminErrorQueueService service, CancellationToken ct)
    {
        var purged = await service.PurgeQueueAsync(topic, subscription, ct);
        return TypedResults.Ok(new PurgeResult(purged));
    }
}
