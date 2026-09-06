using Fpl.Search;
using Fpl.Search.Indexing;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Microsoft.Extensions.Options;

namespace FplBot.EventHandlers;

public class IndexQueryCommandHandler : IConsumer<IndexQuery>
{
    private readonly IIndexingClient _indexingClient;
    private readonly SearchOptions _options;
    private readonly ILogger<IndexQueryCommandHandler> _logger;

    public IndexQueryCommandHandler(
        IIndexingClient indexingClient,
        IOptions<SearchOptions> options,
        ILogger<IndexQueryCommandHandler> logger)
    {
        _indexingClient = indexingClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IndexQuery> context)
    {
        await _indexingClient.Index(new[] { context.Message }, _options.AnalyticsIndex, new CancellationToken());
        _logger.LogInformation("Indexed query \"{query}\"", context.Message.Query);
    }
}
