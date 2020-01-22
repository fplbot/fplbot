using System;
using Fpl.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    internal class GoalMonitorRecurringAction : GameweekRecurringActionBase
    {
        private readonly IPlayerClient _playerClient;
        private readonly IGoalsDuringGameweek _goalsDuringGameweek;
        private readonly ITransfersByGameWeek _transfersByGameWeek;
        private readonly ISlackClient _slackClient;
        private IDictionary<int, int> _currentGoalsByPlayerDuringGameweek;
        private IEnumerable<TransfersByGameWeek.Transfer> _transfersForCurrentGameweek;
        private IEnumerable<User> _users;

        public GoalMonitorRecurringAction(
            IPlayerClient playerClient,
            IGoalsDuringGameweek goalsDuringGameweek,
            ITransfersByGameWeek transfersByGameWeek,
            IOptions<FplbotOptions> options,
            IGameweekClient gwClient,
            ISlackClient slackClient,
            IEnumerable<IPublisher> publishers,
            ILogger<NextGameweekRecurringAction> logger) : 
            base(options, gwClient, publishers, logger)
        {
            _playerClient = playerClient;
            _goalsDuringGameweek = goalsDuringGameweek;
            _transfersByGameWeek = transfersByGameWeek;
            _slackClient = slackClient;
        }

        protected override async Task DoStuffWhenInitialGameweekHasJustBegun(int newGameweek)
        {
            await Reset(newGameweek);
        }     
        
        protected override async Task DoStuffWhenNewGameweekHaveJustBegun(int newGameweek)
        {
            await Reset(newGameweek);
        }

        private async Task Reset(int newGameweek)
        {
            _currentGoalsByPlayerDuringGameweek = null;
            _transfersForCurrentGameweek = await _transfersByGameWeek.GetTransfersByGameweek(newGameweek);
            _users = (await _slackClient.UsersList()).Members;
        }

        protected override async Task DoStuffWithinCurrentGameweek(int currentGameweek)
        {
            var goalsByPlayer = await _goalsDuringGameweek.GetGoalsByPlayerId(currentGameweek);

            if (_currentGoalsByPlayerDuringGameweek == null)
            {
                await ProcessNewGoals(goalsByPlayer);
            }
            else
            {
                await ProcessNewGoals(GoalDiff(_currentGoalsByPlayerDuringGameweek, goalsByPlayer));
            }
            _currentGoalsByPlayerDuringGameweek = goalsByPlayer;
        }

        private static IDictionary<int, int> GoalDiff(IDictionary<int, int> oldGoals, IDictionary<int, int> newGoals)
        {
            var diff = new Dictionary<int, int>();
            foreach (var key in newGoals.Keys)
            {
                if (!oldGoals.ContainsKey(key))
                {
                    diff.Add(key, newGoals[key]);
                } else
                {
                    var goalDiff = newGoals[key] - oldGoals[key];
                    if (goalDiff > 0)
                    {
                        diff.Add(key, newGoals[key] - oldGoals[key]);
                    }
                }
            }

            return diff;
        }

        private async Task ProcessNewGoals(IDictionary<int, int> newGoalsByPlayer)
        {
            var players = await _playerClient.GetAllPlayers();
            foreach (var key in newGoalsByPlayer.Keys)
            {
                var player = players.Single(x => x.Id == key);
                var goals = newGoalsByPlayer[key];
                var message = $"{player.FirstName} {player.SecondName} just scored {(goals == 1 ? "a goal" : $"{goals} goals")}!";

                var entriesTransferredPlayerOut = EntriesThatTransferredPlayerOutThisGameweek(player.Id).ToArray();
                if (entriesTransferredPlayerOut.Any())
                {
                    message += $" Lol, you just transferred him out, {string.Join(", ", entriesTransferredPlayerOut)} :joy:";
                }
                
                await Publish(message);
            }
        }

        private IEnumerable<string> EntriesThatTransferredPlayerOutThisGameweek(int playerId)
        {
            return _transfersForCurrentGameweek == null ? 
                Enumerable.Empty<string>() : 
                _transfersForCurrentGameweek.Where(x => x.PlayerTransferredOut == playerId).Select(x => GetSlackHandle(x.EntryName) ?? x.EntryName);
        }

        private string GetSlackHandle(string entryName)
        {
            var match = _users?.FirstOrDefault(m => string.Equals(m.Real_name, entryName, StringComparison.CurrentCultureIgnoreCase));
            return match != null ? $"@{match.Name}" : null;
        }

        public override string Cron => Constants.CronPatterns.EveryMinute;
    }
}