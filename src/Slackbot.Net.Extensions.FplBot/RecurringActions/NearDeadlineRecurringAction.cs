using System;
using Fpl.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System.Linq;
using System.Threading.Tasks;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    internal class NearDeadlineRecurringAction : IRecurringAction
    {
        private readonly IGameweekClient _gwClient;
        private readonly DateTimeUtils _dateTimeUtils;
        private readonly ISlackWorkSpacePublisher _workspacePublisher;
        private readonly ILogger<NearDeadlineRecurringAction> _logger;
        private readonly int _minutesBeforeDeadline;
  

        public NearDeadlineRecurringAction(
            IGameweekClient gwClient, 
            DateTimeUtils dateTimeUtils, 
            ISlackWorkSpacePublisher workspacePublisher,
            ILogger<NearDeadlineRecurringAction> logger)
        {
            _gwClient = gwClient;
            _dateTimeUtils = dateTimeUtils;
            _workspacePublisher = workspacePublisher;
            _logger = logger;
            _minutesBeforeDeadline = 60;
        }

        public async Task Process()
        {
            var gweeks = await _gwClient.GetGameweeks();
            
            var current = gweeks.FirstOrDefault(gw => gw.IsCurrent);
            
            
            if (current == null)
            {
                _logger.LogDebug($"No current gameweek");
                return;
            }

            if(_dateTimeUtils.IsWithinMinutesToDate(_minutesBeforeDeadline, current.Deadline))
            {
                _logger.LogDebug($"Notifying, since <{_minutesBeforeDeadline} minutes to current (gw{current.Id}) deadline");
                await _workspacePublisher.PublishToAllWorkspaceChannels($"<!channel> Gameweek {current.Id} deadline in 60 minutes!");
                return;
            }
            
            var next = gweeks.FirstOrDefault(gw => gw.IsNext);

            if (next == null)
            {
                _logger.LogDebug($"No next gameweek");
                return;
            }
            
            if(_dateTimeUtils.IsWithinMinutesToDate(_minutesBeforeDeadline, next.Deadline))
            {
                _logger.LogDebug($"Notifying, since <{_minutesBeforeDeadline} minutes to next (gw{next.Id}) deadline");
                await _workspacePublisher.PublishToAllWorkspaceChannels($"<!channel> Gameweek {next.Id} deadline in 60 minutes!");
            }
            
            else
            {
                _logger.LogDebug($"Not {_minutesBeforeDeadline} minutes to deadline.\n" +
                                 $"NowUtc: {DateTime.UtcNow}\n" + 
                                 $"Deadline current:{current.Deadline}\n" +
                                 $"Deadline next:{next.Deadline}\n" +
                                 $"No notification.");
            }
        }
        public string Cron => Constants.CronPatterns.EveryMinuteAt20Seconds;
    }
}