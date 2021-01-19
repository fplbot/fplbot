using CronBackgroundServices;
using Fpl.Search;
using Fpl.Search.Indexing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
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
            _logger.LogInformation($"Registering IndexerRecurringAction. Will run at \"{_options.IndexingCron}\"");
        }

        public async Task Process(CancellationToken stoppingToken)
        {
            using (_logger.BeginCorrelationScope())
            {
                if (!_options.ShouldIndex)
                {
                    _logger.LogInformation("Bypassing the indexing job, since config says so");
                    return;
                }

                stoppingToken.Register(() =>
                {
                    _logger.LogInformation("Cancelling the indexing job due to stoppingToken being cancelled");
                    _indexingService.Cancel();
                });

                _logger.LogInformation("Starting the entries indexing job");
                await _indexingService.IndexEntries();
                _logger.LogInformation("Finished indexing all entries");

                _logger.LogInformation("Starting the league indexing job");
                await _indexingService.IndexLeagues();
                _logger.LogInformation("Finished indexing all leagues");
            }
        }

        public string Cron => _options.IndexingCron;
    }
}