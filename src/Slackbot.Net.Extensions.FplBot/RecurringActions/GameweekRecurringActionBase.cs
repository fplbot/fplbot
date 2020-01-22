using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    internal abstract class GameweekRecurringActionBase : IRecurringAction
    {
        private readonly IOptions<FplbotOptions> _options;
        private readonly IGameweekClient _gwClient;
        private readonly ISlackClientService _slackClientBuilder;
        private readonly ILogger<NextGameweekRecurringAction> _logger;
        private readonly ITokenStore _tokenStore;
        private Gameweek _storedCurrent;

        protected GameweekRecurringActionBase(
            IOptions<FplbotOptions> options,
            IGameweekClient gwClient,
            ISlackClientService slackClientBuilder,
            ILogger<NextGameweekRecurringAction> logger,
            ITokenStore tokenStore)
        {
            _options = options;
            _gwClient = gwClient;
            _slackClientBuilder = slackClientBuilder;
            _logger = logger;
            _tokenStore = tokenStore;
        }

        public async Task Process()
        {
            _logger.LogInformation($"Channel: {_options.Value.Channel} & League: {_options.Value.LeagueId}");

            var gameweeks = await _gwClient.GetGameweeks();
            var fetchedCurrent = gameweeks.FirstOrDefault(gw => gw.IsCurrent);
            if (_storedCurrent == null)
            {
                _logger.LogInformation("Initial fetch executed.");
                _storedCurrent = fetchedCurrent;
                if (fetchedCurrent != null)
                {
                    await DoStuffWhenInitialGameweekHasJustBegun(fetchedCurrent.Id);
                }
            }

            if (fetchedCurrent == null)
            {
                _logger.LogInformation("No gw marked as current");
                return;
            }

            _logger.LogInformation($"Stored: {_storedCurrent.Id} & Fetched: {fetchedCurrent.Id}");

            if (fetchedCurrent.Id > _storedCurrent.Id)
            {
                await DoStuffWhenNewGameweekHaveJustBegun(fetchedCurrent.Id);
            }

            _storedCurrent = fetchedCurrent;

            await DoStuffWithinCurrentGameweek(_storedCurrent.Id);
        }

        protected virtual Task DoStuffWhenInitialGameweekHasJustBegun(int newGameweek) { return Task.CompletedTask; }
        protected virtual Task DoStuffWhenNewGameweekHaveJustBegun(int newGameweek) { return Task.CompletedTask; }
        protected virtual Task DoStuffWithinCurrentGameweek(int currentGameweek) { return Task.CompletedTask; }
        public abstract string Cron { get; }

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
    }
}
