using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Workers;
using Slackbot.Net.Workers.Publishers;

namespace FplBot.ConsoleApps.RecurringActions
{
    public class NextGameweekRecurringAction : RecurringAction
    {
        private readonly IOptions<FplbotOptions> _options;
        private readonly IGameweekClient _gwClient;
        private readonly IEnumerable<IPublisher> _publishers;
        private static string _EveryMinuteCron = "0 */1 * * * *"; 
        private Gameweek _storedCurrent;

        public NextGameweekRecurringAction(IOptions<FplbotOptions> options, IGameweekClient gwClient, IEnumerable<IPublisher> publishers, ILogger<NextGameweekRecurringAction> logger) : base(_EveryMinuteCron, logger)
        {
            _options = options;
            _gwClient = gwClient;
            _publishers = publishers;
        }

        public override async Task Process()
        {
            var gameweeks = await _gwClient.GetGameweeks();
            var fetchedCurrent = gameweeks.FirstOrDefault(gw => gw.IsCurrent);
            if (_storedCurrent == null)
            {
                Logger.LogInformation("Initial fetch executed.");
                _storedCurrent = fetchedCurrent;
            }

            if (fetchedCurrent == null)
            {
                Logger.LogInformation("No gw marked as current");
                return;
            }
            
            Logger.LogInformation($"Stored: {_storedCurrent.Id} & Fetched: {fetchedCurrent.Id}");
            
            if (fetchedCurrent.Id >_storedCurrent.Id)
            {
                foreach (var p in _publishers)
                {
                    await p.Publish(new Notification
                    {
                        Recipient = _options.Value.Channel,
                        Msg = $"Gameweek {fetchedCurrent.Id}!"
                    });
                }
            }

            _storedCurrent = fetchedCurrent;
        }
    }
}