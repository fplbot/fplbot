using System.Collections.Concurrent;

namespace FplBot.WebApi.Admin;

public enum ErrorQueueJobStatus
{
    Queued,
    Running,
    Succeeded,
    Failed
}

public record ErrorQueueJob(
    Guid Id,
    string Queue,
    string Kind,
    ErrorQueueJobStatus Status,
    string? Message,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

// Retry/discard/purge all drain a Service Bus queue over the network, and a queue with any backlog
// takes far longer than a browser is willing to wait on a POST. They run here instead: the endpoint
// hands the work over, answers 202 with a job id, and the caller polls for the outcome. Running
// detached also means the work is no longer tied to the request's CancellationToken — a browser
// navigating away mid-purge used to cancel the drain partway through.
public class AdminErrorQueueJobRunner(IServiceScopeFactory scopeFactory, ILogger<AdminErrorQueueJobRunner> logger)
{
    private static readonly TimeSpan RetentionWindow = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<Guid, ErrorQueueJob> _jobs = new();

    public ErrorQueueJob? Get(Guid id) => _jobs.GetValueOrDefault(id);

    public ErrorQueueJob Start(string queue, string kind, Func<AdminErrorQueueService, CancellationToken, Task<string>> work)
    {
        PruneCompleted();

        var job = new ErrorQueueJob(Guid.NewGuid(), queue, kind, ErrorQueueJobStatus.Queued, null, DateTimeOffset.UtcNow, null);
        _jobs[job.Id] = job;

        _ = Task.Run(async () =>
        {
            Update(job.Id, j => j with { Status = ErrorQueueJobStatus.Running });
            try
            {
                // A fresh scope, not the request's: the request is long gone by the time this runs.
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<AdminErrorQueueService>();
                var message = await work(service, CancellationToken.None);
                Update(job.Id, j => j with
                {
                    Status = ErrorQueueJobStatus.Succeeded,
                    Message = message,
                    CompletedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error queue job {Kind} on {Queue} failed", kind, queue);
                Update(job.Id, j => j with
                {
                    Status = ErrorQueueJobStatus.Failed,
                    Message = e.Message,
                    CompletedAt = DateTimeOffset.UtcNow
                });
            }
        });

        return job;
    }

    private void Update(Guid id, Func<ErrorQueueJob, ErrorQueueJob> update)
    {
        if (_jobs.TryGetValue(id, out var existing))
            _jobs[id] = update(existing);
    }

    private void PruneCompleted()
    {
        var cutoff = DateTimeOffset.UtcNow - RetentionWindow;
        foreach (var (id, job) in _jobs)
        {
            if (job.CompletedAt is { } completed && completed < cutoff)
                _jobs.TryRemove(id, out _);
        }
    }
}
