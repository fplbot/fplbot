using Fpl.Search;
using Fpl.Search.Indexing;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Microsoft.Extensions.Options;

namespace FplBot.EventHandlers;

public class IndexQueryCommandHandler(
    IIndexingClient indexingClient,
    IOptions<SearchOptions> options,
    ILogger<IndexQueryCommandHandler> logger)
    : IConsumer<IndexQuery>
{
    private readonly SearchOptions _options = options.Value;

    public async Task Consume(ConsumeContext<IndexQuery> context)
    {
        await indexingClient.Index([context.Message], _options.AnalyticsIndex, context.CancellationToken);
        logger.LogInformation("Indexed query \"{query}\"", context.Message.Query);
    }
}
