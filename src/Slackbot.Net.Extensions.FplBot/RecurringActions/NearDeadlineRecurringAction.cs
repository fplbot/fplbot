using System;
using Fpl.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CronBackgroundServices;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    internal class NearDeadlineRecurringAction : IRecurringAction
    {
        private readonly IGameweekClient _gwClient;
        private readonly DateTimeUtils _dateTimeUtils;
        private readonly ISlackWorkSpacePublisher _workspacePublisher;
        private readonly ILogger<NearDeadlineRecurringAction> _logger;
        private readonly int _minutesBeforeDeadline1Hour = 60;
        private readonly int _minutesBeforeDeadline1Day = 60 * 24;
  

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
        }

        public async Task Process(CancellationToken token)
        {
            using (_logger.BeginCorrelationScope())
            {
                _logger.LogInformation($"Running {nameof(NearDeadlineRecurringAction)}");
                var gweeks = await _gwClient.GetGameweeks();
            
                var current = gweeks.FirstOrDefault(gw => gw.IsCurrent);
                
                
                if (current == null)
                {
                    current = gweeks.First();
                }

                if(_dateTimeUtils.IsWithinMinutesToDate(_minutesBeforeDeadline1Hour, current.Deadline))
                {
                    _logger.LogInformation($"Notifying, since <{_minutesBeforeDeadline1Hour} minutes to current (gw{current.Id}) deadline");
                    await _workspacePublisher.PublishToAllWorkspaceChannels($"<!channel> ⏳ Gameweek {current.Id} deadline in 60 minutes!");
                    return;
                }
                
                if(_dateTimeUtils.IsWithinMinutesToDate(_minutesBeforeDeadline1Day, current.Deadline))
                {
                    _logger.LogInformation($"Notifying, since <{_minutesBeforeDeadline1Day} minutes to current (gw{current.Id}) deadline");
                    await _workspacePublisher.PublishToAllWorkspaceChannels($"⏳ Gameweek {current.Id} deadline in 24 hours!");
                    return;
                }
                
                var next = gweeks.FirstOrDefault(gw => gw.IsNext);

                if (next == null)
                {
                    _logger.LogInformation($"No next gameweek");
                    return;
                }
                
                if(_dateTimeUtils.IsWithinMinutesToDate(_minutesBeforeDeadline1Hour, next.Deadline))
                {
                    _logger.LogInformation($"Notifying, since <{_minutesBeforeDeadline1Hour} minutes to next (gw{next.Id}) deadline");
                    await _workspacePublisher.PublishToAllWorkspaceChannels($"<!channel> ⏳ Gameweek {next.Id} deadline in 60 minutes!");
                    return;
                }
                
                if(_dateTimeUtils.IsWithinMinutesToDate(_minutesBeforeDeadline1Day, next.Deadline))
                {
                    _logger.LogInformation($"Notifying, since <{_minutesBeforeDeadline1Day} minutes to next (gw{next.Id}) deadline");
                    await _workspacePublisher.PublishToAllWorkspaceChannels($"⏳ Gameweek {next.Id} deadline in 24 hours!");
                    return;
                }
              
                _logger.LogDebug($"Not {_minutesBeforeDeadline1Hour}/{_minutesBeforeDeadline1Day} minutes to deadline.\n" +
                                 $"NowUtc: {DateTime.UtcNow}\n" + 
                                 $"Deadline current:{current.Deadline}\n" +
                                 $"Deadline next:{next.Deadline}\n" +
                                 $"No notification.");
                
            }
        }
        public string Cron => Constants.CronPatterns.EveryMinuteAt20Seconds;
    }
}