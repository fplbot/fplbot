using System.Threading;
using System.Threading.Tasks;
using CronBackgroundServices;
using Fpl.Search.Indexing;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    public class IndexerRecurringAction : IRecurringAction
    {
        public IIndexingClient IndexingClient { get; }

        public IndexerRecurringAction(IIndexingClient indexingClient)
        {
            IndexingClient = indexingClient;
        }


        public Task Process(CancellationToken stoppingToken)
        {
            throw new System.NotImplementedException();
        }

        public string Cron => Constants.CronPatterns.EveryThursdayAtMidnight;
    }
}