using FplBot.WebApi.Admin;

namespace FplBot.WebApi.Endpoints.Api.Admin;

public record PurgeResult(int Purged);

public static class AdminErrorEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/errors/queues", GetQueues);
        group.MapGet("/errors/queues/{queue}/messages", GetMessages);
        group.MapPost("/errors/queues/{queue}/messages/{messageId}/retry", RetryMessage);
        group.MapPost("/errors/queues/{queue}/messages/{messageId}/discard", DiscardMessage);
        group.MapPost("/errors/queues/{queue}/purge", PurgeQueue);
    }

    internal static async Task<IResult> GetQueues(AdminErrorQueueService service, CancellationToken ct)
        => TypedResults.Ok(await service.ListQueuesAsync(ct));

    internal static async Task<IResult> GetMessages(string queue, AdminErrorQueueService service, CancellationToken ct)
    {
        if (!AdminErrorQueueService.IsErrorQueue(queue))
            return TypedResults.BadRequest<object>(new { message = $"'{queue}' is not an error queue." });

        return TypedResults.Ok(await service.PeekMessagesAsync(queue, ct: ct));
    }

    internal static async Task<IResult> RetryMessage(string queue, string messageId, AdminErrorQueueService service, CancellationToken ct)
    {
        if (!AdminErrorQueueService.IsErrorQueue(queue))
            return TypedResults.BadRequest<object>(new { message = $"'{queue}' is not an error queue." });

        var retried = await service.RetryMessageAsync(queue, messageId, ct);
        return retried ? TypedResults.Ok(new { message = "Message retried" }) : TypedResults.NotFound();
    }

    internal static async Task<IResult> DiscardMessage(string queue, string messageId, AdminErrorQueueService service, CancellationToken ct)
    {
        if (!AdminErrorQueueService.IsErrorQueue(queue))
            return TypedResults.BadRequest<object>(new { message = $"'{queue}' is not an error queue." });

        var discarded = await service.DiscardMessageAsync(queue, messageId, ct);
        return discarded ? TypedResults.Ok(new { message = "Message discarded" }) : TypedResults.NotFound();
    }

    internal static async Task<IResult> PurgeQueue(string queue, AdminErrorQueueService service, CancellationToken ct)
    {
        if (!AdminErrorQueueService.IsErrorQueue(queue))
            return TypedResults.BadRequest<object>(new { message = $"'{queue}' is not an error queue." });

        var purged = await service.PurgeQueueAsync(queue, ct);
        return TypedResults.Ok(new PurgeResult(purged));
    }
}
