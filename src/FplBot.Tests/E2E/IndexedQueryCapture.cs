using System.Collections.Concurrent;
using Fpl.Search.Indexing;
using FplBot.Messaging.Contracts.Commands.v1;

namespace FplBot.Tests.E2E;

public class IndexedQueryCapture : IIndexingClient
{
    private readonly ConcurrentQueue<IndexQuery> _queries = new();

    public IReadOnlyCollection<IndexQuery> Queries => [.. _queries];

    public void Reset() => _queries.Clear();

    public Task Index<T>(IEnumerable<T> items, string index, CancellationToken token) where T : class
    {
        foreach (var query in items.OfType<IndexQuery>())
        {
            _queries.Enqueue(query);
        }

        return Task.CompletedTask;
    }
}
