using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Publishers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    internal abstract class GameweekRecurringActionBase : IRecurringAction
    {
        private readonly IOptions<FplbotOptions> _options;
        private readonly IGameweekClient _gwClient;
        private readonly IEnumerable<IPublisher> _publishers;
        private readonly ILogger<NextGameweekRecurringAction> _logger;
        private Gameweek _storedCurrent;

        protected GameweekRecurringActionBase(
            IOptions<FplbotOptions> options,
            IGameweekClient gwClient,
            IEnumerable<IPublisher> publishers,
            ILogger<NextGameweekRecurringAction> logger)
        {
            _options = options;
            _gwClient = gwClient;
            _publishers = publishers;
            _logger = logger;
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

        protected async Task Publish(string msg)
        {
            foreach (var p in _publishers)
            {
                await p.Publish(new Notification
                {
                    Recipient = _options.Value.Channel,
                    Msg = msg
                });
            }
        }
    }
}
