using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    internal class NearDeadlineRecurringAction : IRecurringAction
    {
        private readonly IOptions<FplbotOptions> _options;
        private readonly IGameweekClient _gwClient;
        private readonly DateTimeUtils _dateTimeUtils;
        private readonly IEnumerable<IPublisher> _publishers;
        private readonly ILogger<NearDeadlineRecurringAction> _logger;
        private readonly int _minutesBeforeDeadline;
        private const string EveryMinuteCron = "0 */1 * * * *";

        public NearDeadlineRecurringAction(IOptions<FplbotOptions> options, IGameweekClient gwClient, DateTimeUtils dateTimeUtils, IEnumerable<IPublisher> publishers, ILogger<NearDeadlineRecurringAction> logger)
        {
            _options = options;
            _gwClient = gwClient;
            _dateTimeUtils = dateTimeUtils;
            _publishers = publishers;
            _logger = logger;
            _minutesBeforeDeadline = 60;
        }

        public async Task Process()
        {
            var gweeks = await _gwClient.GetGameweeks();
            var next = gweeks.FirstOrDefault(gw => gw.IsNext);
            
            if (next == null)
            {
                _logger.LogDebug($"No next gameweek");
                return;
            }

            if(_dateTimeUtils.IsWithinMinutesToDate(_minutesBeforeDeadline, next.Deadline))
            {
                _logger.LogDebug($"Notifying, since <{_minutesBeforeDeadline} minutes to deadline");
                await Publish($"<!channel> Gameweek {next.Id} deadline in 1 hour!");
            }
            else
            {
                _logger.LogDebug($">{_minutesBeforeDeadline} minutes to deadline.\nNowUtc: {_dateTimeUtils.NowUtc}\nDeadline:{next.Deadline}\nNo notification.");
            }
        }
        
        private async Task Publish(string msg)
        {
            foreach (var p in _publishers)
            {
                await p.Publish(new Notification
                {
                    Msg = msg,
                    Recipient = _options.Value.Channel
                });
            }
        }

        public string Cron => EveryMinuteCron;
    }
}