using FplBot.WebApi.Admin;

namespace FplBot.WebApi.Endpoints.Api.Admin;

public record ErrorQueueJobAccepted(Guid JobId, string Kind, string Queue);

public record ErrorQueueJobState(Guid JobId, string Kind, string Queue, string Status, string? Message);

public static class AdminErrorEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/errors/queues", GetQueues);
        group.MapGet("/errors/queues/{queue}/messages", GetMessages);
        group.MapPost("/errors/queues/{queue}/messages/{messageId}/retry", RetryMessage);
        group.MapPost("/errors/queues/{queue}/messages/{messageId}/discard", DiscardMessage);
        group.MapPost("/errors/queues/{queue}/retry-all", RetryAllMessages);
        group.MapPost("/errors/queues/{queue}/purge", PurgeQueue);
        group.MapGet("/errors/jobs/{jobId:guid}", GetJob);
    }

    internal static async Task<IResult> GetQueues(AdminErrorQueueService service, CancellationToken ct)
        => TypedResults.Ok(await service.ListQueuesAsync(ct));

    internal static async Task<IResult> GetMessages(string queue, AdminErrorQueueService service, CancellationToken ct)
    {
        if (!AdminErrorQueueService.IsErrorQueue(queue))
            return TypedResults.BadRequest<object>(new { message = $"'{queue}' is not an error queue." });

        return TypedResults.Ok(await service.PeekMessagesAsync(queue, ct: ct));
    }

    internal static IResult RetryMessage(string queue, string messageId, AdminErrorQueueJobRunner jobs)
        => StartJob(queue, "retry", jobs, async (service, ct) =>
        {
            var retried = await service.RetryMessageAsync(queue, messageId, ct);
            return retried
                ? "Message retried."
                // A scan that comes up empty is a real outcome worth naming: the id the operator
                // clicked is no longer in the queue, which a stale list makes easy to hit.
                : "That message is no longer in the queue — it may already have been retried or discarded.";
        });

    internal static IResult DiscardMessage(string queue, string messageId, AdminErrorQueueJobRunner jobs)
        => StartJob(queue, "discard", jobs, async (service, ct) =>
        {
            var discarded = await service.DiscardMessageAsync(queue, messageId, ct);
            return discarded
                ? "Message discarded."
                : "That message is no longer in the queue — it may already have been retried or discarded.";
        });

    internal static IResult RetryAllMessages(string queue, AdminErrorQueueJobRunner jobs)
        => StartJob(queue, "retry-all", jobs, async (service, ct) =>
        {
            var retried = await service.RetryAllMessagesAsync(queue, ct);
            return $"Retried {retried} message(s).";
        });

    internal static IResult PurgeQueue(string queue, AdminErrorQueueJobRunner jobs)
        => StartJob(queue, "purge", jobs, async (service, ct) =>
        {
            var purged = await service.PurgeQueueAsync(queue, ct);
            return $"Purged {purged} message(s).";
        });

    internal static IResult GetJob(Guid jobId, AdminErrorQueueJobRunner jobs)
    {
        var job = jobs.Get(jobId);
        return job is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new ErrorQueueJobState(job.Id, job.Kind, job.Queue, job.Status.ToString(), job.Message));
    }

    // Draining a Service Bus queue takes as long as it takes, so every mutating action starts a
    // background job and answers 202 immediately rather than holding the request open. The caller
    // polls the Location URL for the outcome.
    private static IResult StartJob(
        string queue, string kind, AdminErrorQueueJobRunner jobs,
        Func<AdminErrorQueueService, CancellationToken, Task<string>> work)
    {
        if (!AdminErrorQueueService.IsErrorQueue(queue))
            return TypedResults.BadRequest<object>(new { message = $"'{queue}' is not an error queue." });

        var job = jobs.Start(queue, kind, work);
        return TypedResults.Accepted($"/api/admin/errors/jobs/{job.Id}", new ErrorQueueJobAccepted(job.Id, kind, queue));
    }
}
