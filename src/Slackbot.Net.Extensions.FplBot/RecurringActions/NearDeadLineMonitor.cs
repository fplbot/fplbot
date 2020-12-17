using System;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    internal class NearDeadLineMonitor
    {
        private readonly IGameweekClient _gwClient;
        private readonly ILogger<NearDeadLineMonitor> _logger;
        
        public event Func<Gameweek, Task> MinuteTickHandlers = (gw) => Task.CompletedTask;
        public NearDeadLineMonitor(IGameweekClient gwClient, ILogger<NearDeadLineMonitor> logger)
        {
            _gwClient = gwClient;
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
                await MinuteTickHandlers(current);
            }

            var next = gweeks.FirstOrDefault(gw => gw.IsNext);

            if (next != null)
            {
                await MinuteTickHandlers(next);
            }
            else
            {
                _logger.LogInformation($"No next gameweek");
            }
        }
    }
}