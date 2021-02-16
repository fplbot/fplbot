using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using FplBot.Core.Helpers;
using FplBot.Core.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FplBot.Core.RecurringActions
{
    internal class NearDeadLineMonitor
    {
        private readonly IGameweekClient _gwClient;
        private readonly DateTimeUtils _dateTimeUtils;
        private readonly IMediator _mediator;
        private readonly ILogger<NearDeadLineMonitor> _logger;
        
        public NearDeadLineMonitor(IGameweekClient gwClient, DateTimeUtils dateTimeUtils, IMediator mediator, ILogger<NearDeadLineMonitor> logger)
        {
            _gwClient = gwClient;
            _dateTimeUtils = dateTimeUtils;
            _mediator = mediator;
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
                    await _mediator.Publish(new OneHourToDeadline(current));

                if (_dateTimeUtils.IsWithinMinutesToDate(24*60, current.Deadline))
                    await _mediator.Publish(new TwentyFourHoursToDeadline(current));
            }

            var next = gweeks.FirstOrDefault(gw => gw.IsNext);

            if (next != null)
            {
                if (_dateTimeUtils.IsWithinMinutesToDate(60, next.Deadline))
                    await _mediator.Publish(new OneHourToDeadline(next));
                
                if (_dateTimeUtils.IsWithinMinutesToDate(24*60, next.Deadline))
                    await _mediator.Publish(new TwentyFourHoursToDeadline(next));
            }
            else
            {
                _logger.LogInformation($"No next gameweek");
            }
        }
    }
}