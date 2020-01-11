using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    internal class NextGameweekRecurringAction : IRecurringAction
    {
        private readonly IOptions<FplbotOptions> _options;
        private readonly IGameweekClient _gwClient;
        private readonly IEnumerable<IPublisher> _publishers;
        private readonly ICaptainsByGameWeek _captainsByGameweek;
        private readonly ILogger<NextGameweekRecurringAction> _logger;
        private const string EveryMinuteCron = "0 */1 * * * *";
        private Gameweek _storedCurrent;

        public NextGameweekRecurringAction(IOptions<FplbotOptions> options, IGameweekClient gwClient, IEnumerable<IPublisher> publishers, ICaptainsByGameWeek captainsByGameweek, ILogger<NextGameweekRecurringAction> logger)
        {
            _options = options;
            _gwClient = gwClient;
            _publishers = publishers;
            _captainsByGameweek = captainsByGameweek;
            _logger = logger;
        }

        public async Task Process()
        {
            _logger.LogInformation($"Channel: {_options.Value.Channel} & League: {_options.Value.LeagueId}");

            var gameweeks = await _gwClient.GetGameweeks();
            var fetchedCurrent = gameweeks.FirstOrDefault(gw => gw.IsCurrent);
            if (_storedCurrent == null)
            {
                _logger.LogInformation("Initial fetch executed.");
                _storedCurrent = fetchedCurrent;
            }

            if (fetchedCurrent == null)
            {
                _logger.LogInformation("No gw marked as current");
                return;
            }
            
            _logger.LogInformation($"Stored: {_storedCurrent.Id} & Fetched: {fetchedCurrent.Id}");
            
            if (fetchedCurrent.Id >_storedCurrent.Id)
            {
                var captains = await _captainsByGameweek.GetCaptainsByGameWeek(fetchedCurrent.Id);
                foreach (var p in _publishers)
                {
                    await p.Publish(new Notification
                    {
                        Recipient = _options.Value.Channel,
                        Msg = $"Gameweek {fetchedCurrent.Id}! \n {captains}"
                    });
                }
            }

            _storedCurrent = fetchedCurrent;
        }

        public string Cron => EveryMinuteCron;
    }
}