using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using FplBot.Messaging.Contracts.Commands.v1;
using NServiceBus;

namespace Fpl.Search.Indexing
{
    public class QueryAnalyticsIndexingService : IHandleMessages<IndexQuery>
    {
        private readonly IIndexingClient _indexingClient;
        private readonly SearchOptions _options;
        private readonly ILogger<QueryAnalyticsIndexingService> _logger;

        public QueryAnalyticsIndexingService(
            IIndexingClient indexingClient,
            IOptions<SearchOptions> options,
            ILogger<QueryAnalyticsIndexingService> logger)
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
}