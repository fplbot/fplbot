using System;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Helpers;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    internal class NearDeadLineMonitor
    {
        private readonly IGameweekClient _gwClient;
        private readonly DateTimeUtils _dateTimeUtils;
        private readonly ILogger<NearDeadLineMonitor> _logger;
        public event Func<Gameweek, Task> TwentyFourHoursToDeadlineHandlers = _ => Task.CompletedTask;
        public event Func<Gameweek, Task> OneHourToDeadlineHandlers = _ => Task.CompletedTask;
        public NearDeadLineMonitor(IGameweekClient gwClient, DateTimeUtils dateTimeUtils, ILogger<NearDeadLineMonitor> logger)
        {
            _gwClient = gwClient;
            _dateTimeUtils = dateTimeUtils;
            _logger = logger;
        }

        public async Task EveryMinuteTick()
        {
            var gweeks = await _gwClient.GetGameweeks();

            var current = gweeks.FirstOrDefault(gw => gw.IsCurrent);

            if (current == null)
            {
                current = gweeks.First();
            }

            if (current != null)
            {
                if (_dateTimeUtils.IsWithinMinutesToDate(60, current.Deadline))
                    await OneHourToDeadlineHandlers(current);
                
                if (_dateTimeUtils.IsWithinMinutesToDate(24*60, current.Deadline))
                    await TwentyFourHoursToDeadlineHandlers(current);
            }

            var next = gweeks.FirstOrDefault(gw => gw.IsNext);

            if (next != null)
            {
                if (_dateTimeUtils.IsWithinMinutesToDate(60, next.Deadline))
                    await OneHourToDeadlineHandlers(next);
                
                if (_dateTimeUtils.IsWithinMinutesToDate(24*60, next.Deadline))
                    await TwentyFourHoursToDeadlineHandlers(next);
            }
            else
            {
                _logger.LogInformation($"No next gameweek");
            }
        }
    }
}