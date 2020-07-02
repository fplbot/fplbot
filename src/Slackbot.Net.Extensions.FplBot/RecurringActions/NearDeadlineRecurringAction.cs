using System;
using Fpl.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.SlackClients.Http;
using System.Linq;
using System.Threading.Tasks;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    internal class NearDeadlineRecurringAction : IRecurringAction
    {
        private readonly IGameweekClient _gwClient;
        private readonly DateTimeUtils _dateTimeUtils;
        private readonly ISlackClientBuilder _slackClientBuilder;
        private readonly ILogger<NearDeadlineRecurringAction> _logger;
        private readonly int _minutesBeforeDeadline;
        private readonly ITokenStore _tokenStore;
        private readonly IFetchFplbotSetup _teamRepo;

        public NearDeadlineRecurringAction(
            IGameweekClient gwClient, 
            DateTimeUtils dateTimeUtils, 
            ISlackClientBuilder slackClientBuilder, 
            ILogger<NearDeadlineRecurringAction> logger, 
            ITokenStore tokenStore, 
            IFetchFplbotSetup teamRepo)
        {
            _gwClient = gwClient;
            _dateTimeUtils = dateTimeUtils;
            _slackClientBuilder = slackClientBuilder;
            _logger = logger;
            _tokenStore = tokenStore;
            _teamRepo = teamRepo;
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
                await Publish($"<!channel> Gameweek {current.Id} deadline in 1 hour!");
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
                await Publish($"<!channel> Gameweek {next.Id} deadline in 1 hour!");
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

        protected async Task Publish(string msg)
        {
            var tokens = await _tokenStore.GetTokens();
            foreach (var token in tokens)
            {
                var setup = await _teamRepo.GetSetupByToken(token);

                var slackClient = _slackClientBuilder.Build(token);
                var res = await slackClient.ChatPostMessage(setup.Channel, msg);

                if (!res.Ok)
                    _logger.LogError($"Could not post to {setup.Channel}", res.Error);
            }
        }

        public string Cron => Constants.CronPatterns.EveryMinuteAt20Seconds;
    }
}