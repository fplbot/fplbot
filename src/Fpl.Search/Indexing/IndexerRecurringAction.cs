using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CronBackgroundServices;
using Fpl.Search;
using Fpl.Search.Indexing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FplBot.Core.RecurringActions
{
    public class IndexerRecurringAction : IRecurringAction
    {
        private readonly IIndexingService _indexingService;
        private readonly ILogger<IndexerRecurringAction> _logger;
        private readonly SearchOptions _options;

        public IndexerRecurringAction(IIndexingService indexingService, ILogger<IndexerRecurringAction> logger, IOptions<SearchOptions> options)
        {
            _indexingService = indexingService;
            _logger = logger;
            _options = options.Value;
        }

        public async Task Process(CancellationToken stoppingToken)
        {
            using (_logger.BeginScope(new Dictionary<string, object> {["CorrelationId"] = Guid.NewGuid()}))
            {
                if (!_options.ShouldIndexEntries && !_options.ShouldIndexLeagues)
                {
                    _logger.LogInformation("Bypassing the indexing job, since config says so");
                    return;
                }

                if (_options.ShouldIndexEntries)
                {
                    _logger.LogInformation("Starting the entries indexing job");
                    await _indexingService.IndexEntries(stoppingToken);
                    _logger.LogInformation("Finished indexing all entries");
                }

                if (_options.ShouldIndexLeagues)
                {
                    _logger.LogInformation("Starting the league indexing job");
                    await _indexingService.IndexLeagues(stoppingToken);
                    _logger.LogInformation("Finished indexing all leagues");
                }
            }
        }

        public string Cron => _options.IndexingCron;
    }
}