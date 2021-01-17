using CronBackgroundServices;
using Fpl.Search.Indexing;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    public class IndexerRecurringAction : IRecurringAction
    {
        private readonly IIndexingService _indexingService;
        private readonly ILogger<IndexerRecurringAction> _logger;

        public IndexerRecurringAction(IIndexingService indexingService, ILogger<IndexerRecurringAction> logger)
        {
            _indexingService = indexingService;
            _logger = logger;
        }

        public async Task Process(CancellationToken stoppingToken)
        {
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

        public string Cron => Constants.CronPatterns.EverySundayAtMidnight;
    }
}