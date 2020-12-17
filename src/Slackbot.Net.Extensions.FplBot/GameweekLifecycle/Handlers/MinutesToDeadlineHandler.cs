using System.Threading.Tasks;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    public class MinutesToDeadlineHandler
    {
        private readonly ISlackWorkSpacePublisher _workspacePublisher;
        private readonly ILogger<MinutesToDeadlineHandler> _logger;
        private readonly int _minutesBeforeDeadline1Hour = 60;
        private readonly int _minutesBeforeDeadline1Day = 60 * 24;
        private readonly DateTimeUtils _dateTimeUtils;
        
        public MinutesToDeadlineHandler(ISlackWorkSpacePublisher workspacePublisher, DateTimeUtils dateTimeUtils, ILogger<MinutesToDeadlineHandler> logger)
        {
            _workspacePublisher = workspacePublisher;
            _logger = logger;
            _dateTimeUtils = dateTimeUtils;
        }

        public async Task HandleMinutesTick(Gameweek gameweek)
        {
            var timeToDeadline = (gameweek.Deadline - _dateTimeUtils.NowUtc);
            var gwStatus = gameweek.IsCurrent ? "current" : gameweek.IsNext ? "next" : "first";
            _logger.LogDebug($"{timeToDeadline:g} to {gwStatus} deadline");
            if (_dateTimeUtils.IsWithinMinutesToDate(_minutesBeforeDeadline1Day, gameweek.Deadline))
            {
                _logger.LogInformation($"Notifying about 24h to {gwStatus} (gw{gameweek.Id}) deadline");
                await _workspacePublisher.PublishToAllWorkspaceChannels($"⏳ Gameweek {gameweek.Id} deadline in 24 hours!");
            }
            else if (_dateTimeUtils.IsWithinMinutesToDate(_minutesBeforeDeadline1Hour, gameweek.Deadline))
            {
                _logger.LogInformation($"Notifying about 60 minutes to {gwStatus} (gw{gameweek.Id}) deadline");
                await _workspacePublisher.PublishToAllWorkspaceChannels($"<!channel> ⏳ Gameweek {gameweek.Id} deadline in 60 minutes!");
            }
            else
            {
                _logger.LogDebug($"No notification");
            }
        }
    }
}