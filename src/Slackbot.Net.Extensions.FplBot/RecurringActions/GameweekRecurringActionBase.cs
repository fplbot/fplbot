using System;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.SlackClients.Http;
using System.Linq;
using System.Threading.Tasks;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    internal abstract class GameweekRecurringActionBase : IRecurringAction
    {
        private readonly IOptions<FplbotOptions> _options;
        private readonly IGameweekClient _gwClient;
        private readonly ILogger<GameweekRecurringActionBase> _logger;
        private readonly ITokenStore _tokenStore;
        private readonly ISlackClientBuilder _slackClientBuilder;
        private Gameweek _storedCurrent;
        private IFetchFplbotSetup _teamRepo;

        protected GameweekRecurringActionBase(
            IOptions<FplbotOptions> options,
            IGameweekClient gwClient,
            ILogger<GameweekRecurringActionBase> logger,
            ITokenStore tokenStore,
            ISlackClientBuilder slackClientBuilder)
        {
            _options = options;
            _gwClient = gwClient;
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
                if (fetchedCurrent != null)
                {
                    await DoStuffWhenInitialGameweekHasJustBegun(fetchedCurrent.Id);
                }
            }

            if (fetchedCurrent == null)
            {
                _logger.LogDebug("No gw marked as current");
                return;
            }

            _logger.LogDebug($"Stored: {_storedCurrent.Id} & Fetched: {fetchedCurrent.Id}");

            if (fetchedCurrent.Id > _storedCurrent.Id)
            {
                await DoStuffWhenNewGameweekHaveJustBegun(fetchedCurrent.Id);
            }

            _storedCurrent = fetchedCurrent;

            await DoStuffWithinCurrentGameweek(_storedCurrent.Id, _storedCurrent.IsFinished);
        }

        protected virtual Task DoStuffWhenInitialGameweekHasJustBegun(int newGameweek) { return Task.CompletedTask; }
        protected virtual Task DoStuffWhenNewGameweekHaveJustBegun(int newGameweek) { return Task.CompletedTask; }
        protected virtual Task DoStuffWithinCurrentGameweek(int currentGameweek, bool isFinished) { return Task.CompletedTask; }
        public abstract string Cron { get; }

        protected async Task Publish(Func<ISlackClient, Task<string>> msg)
        {
            var tokens = await _tokenStore.GetTokens();
            foreach (var token in tokens)
            {
                var setup = await _teamRepo.GetSetupByToken(token);
                
                var slackClient = _slackClientBuilder.Build(token);
                
                var res = await slackClient.ChatPostMessage(setup.Channel, await msg(slackClient));

                if (!res.Ok)
                    _logger.LogError($"Could not post to {setup.Channel}", res.Error);
            }
        }
    }
}
