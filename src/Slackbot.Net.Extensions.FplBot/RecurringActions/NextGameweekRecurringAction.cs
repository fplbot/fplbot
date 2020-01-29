using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.SlackClients.Http;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    internal class NextGameweekRecurringAction : IRecurringAction
    {
        private readonly IOptions<FplbotOptions> _options;
        private readonly IGameweekClient _gwClient;
        private readonly ICaptainsByGameWeek _captainsByGameweek;
        private readonly ITransfersByGameWeek _transfersByGameweek;
        private readonly ILogger<NextGameweekRecurringAction> _logger;
        private const string EveryMinuteCron = "0 */1 * * * *";
        private Gameweek _storedCurrent;
        private readonly ITokenStore _tokenStore;
        private readonly ISlackClientBuilder _slackClientBuilder;

        public NextGameweekRecurringAction(IOptions<FplbotOptions> options, IGameweekClient gwClient, ICaptainsByGameWeek captainsByGameweek, ITransfersByGameWeek transfersByGameweek, ILogger<NextGameweekRecurringAction> logger, ITokenStore tokenStore, ISlackClientBuilder slackClientBuilder)
        {
            _options = options;
            _gwClient = gwClient;
            _captainsByGameweek = captainsByGameweek;
            _transfersByGameweek = transfersByGameweek;
            _logger = logger;
            _tokenStore = tokenStore;
            _slackClientBuilder = slackClientBuilder;
        }

        public async Task Process()
        {
            _logger.LogDebug($"Channel: {_options.Value.Channel} & League: {_options.Value.LeagueId}");

            var gameweeks = await _gwClient.GetGameweeks();
            var fetchedCurrent = gameweeks.FirstOrDefault(gw => gw.IsCurrent);
            if (_storedCurrent == null)
            {
                _logger.LogDebug("Initial fetch executed.");
                _storedCurrent = fetchedCurrent;
            }

            if (fetchedCurrent == null)
            {
                _logger.LogDebug("No gw marked as current");
                return;
            }
            
            _logger.LogInformation($"Stored: {_storedCurrent.Id} & Fetched: {fetchedCurrent.Id}");
            
            if (fetchedCurrent.Id >_storedCurrent.Id)
            {
                await Publish($"Gameweek {fetchedCurrent.Id}!");

                var captains = await _captainsByGameweek.GetCaptainsByGameWeek(fetchedCurrent.Id);
                await Publish(captains);
                
                var transfers = await _transfersByGameweek.GetTransfersByGameweek(fetchedCurrent.Id);
                await Publish(transfers);
            }

            _storedCurrent = fetchedCurrent;
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