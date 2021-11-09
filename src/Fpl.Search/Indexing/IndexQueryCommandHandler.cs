using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FplBot.Messaging.Contracts.Commands.v1;
using NServiceBus;

namespace Fpl.Search.Indexing;

public class IndexQueryCommandHandler : IHandleMessages<IndexQuery>
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

    public async Task Handle(IndexQuery message, IMessageHandlerContext context)
    {
        await _indexingClient.Index(new[] { message }, _options.AnalyticsIndex, new CancellationToken());
        _logger.LogInformation("Indexed query \"{query}\"", message.Query);
    }
}