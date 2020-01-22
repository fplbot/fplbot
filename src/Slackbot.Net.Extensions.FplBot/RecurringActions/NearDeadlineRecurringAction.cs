using Fpl.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.SlackClients.Http;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    internal class NearDeadlineRecurringAction : IRecurringAction
    {
        private readonly IOptions<FplbotOptions> _options;
        private readonly IGameweekClient _gwClient;
        private readonly DateTimeUtils _dateTimeUtils;
        private readonly ISlackClientBuilder _slackClientBuilder;
        private readonly ILogger<NearDeadlineRecurringAction> _logger;
        private readonly int _minutesBeforeDeadline;
        private readonly ITokenStore _tokenStore;
        private const string EveryMinuteCron = "0 */1 * * * *";

        public NearDeadlineRecurringAction(IOptions<FplbotOptions> options, IGameweekClient gwClient, DateTimeUtils dateTimeUtils, ISlackClientBuilder slackClientBuilder, ILogger<NearDeadlineRecurringAction> logger, ITokenStore tokenStore)
        {
            _options = options;
            _gwClient = gwClient;
            _dateTimeUtils = dateTimeUtils;
            _slackClientBuilder = slackClientBuilder;
            _logger = logger;
            _tokenStore = tokenStore;
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
                await Publish("TEST");
                _logger.LogDebug($"Not {_minutesBeforeDeadline} minutes to deadline.\n" +
                                 $"NowUtc: {_dateTimeUtils.NowUtc}\n" + 
                                 $"Deadline current:{current.Deadline}\n" +
                                 $"Deadline next:{next.Deadline}\n" +
                                 $"No notification.");
            }
        }
        
        private async Task Publish(string msg)
        {
            var tokens = await _tokenStore.GetTokens();
            foreach (var token in tokens)
            {
                var slackClient = _slackClientBuilder.Build(token);
                //TODO: Fetch channel to post to from some storage for distributed app
                var res = await slackClient.ChatPostMessage(_options.Value.Channel, msg);
                
                if (!res.Ok) 
                    _logger.LogError($"Could not post to {_options.Value.Channel}", res.Error);
            }
        }

        public string Cron => EveryMinuteCron;
    }
}