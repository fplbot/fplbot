using CronBackgroundServices;
using Fpl.Search;
using Fpl.Search.Indexing;
using Microsoft.Extensions.Options;

namespace FplBot.Core.RecurringActions;

public class IndexerRecurringAction(
    IIndexingService indexingService,
    ILogger<IndexerRecurringAction> logger,
    IOptions<SearchOptions> options)
    : IRecurringAction
{
    private readonly SearchOptions _options = options.Value;

    public async Task Process(CancellationToken stoppingToken)
    {
        using (logger.BeginScope(new Dictionary<string, object> {["CorrelationId"] = Guid.NewGuid()}))
        {
            if (!_options.ShouldIndexEntries && !_options.ShouldIndexLeagues)
            {
                logger.LogInformation("Bypassing the indexing job, since config says so");
                return;
            }

            if (_options.ShouldIndexEntries)
            {
                logger.LogInformation("Starting the entries indexing job");
                await indexingService.IndexEntries(stoppingToken);
                logger.LogInformation("Finished indexing all entries");
            }

            if (_options.ShouldIndexLeagues)
            {
                logger.LogInformation("Starting the league indexing job");
                await indexingService.IndexLeagues(stoppingToken);
                logger.LogInformation("Finished indexing all leagues");
            }
        }
    }

    public string Cron => _options.IndexingCron;
}
