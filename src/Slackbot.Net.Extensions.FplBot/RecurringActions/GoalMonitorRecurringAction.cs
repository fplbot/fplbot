using Fpl.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;
using System;
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
        private IDictionary<int, int> _currentGoalsByPlayerDuringGameweek;
        private IEnumerable<TransfersByGameWeek.Transfer> _transfersForCurrentGameweek;

        public GoalMonitorRecurringAction(
            IOptions<FplbotOptions> options,
            IGameweekClient gwClient,
            ILogger<NextGameweekRecurringAction> logger,
            ITokenStore tokenStore,
            ISlackClientBuilder slackClientBuilder,
            IPlayerClient playerClient,
            IGoalsDuringGameweek goalsDuringGameweek,
            ITransfersByGameWeek transfersByGameWeek) : 
            base(options, gwClient, logger, tokenStore, slackClientBuilder)
        {
            _playerClient = playerClient;
            _goalsDuringGameweek = goalsDuringGameweek;
            _transfersByGameWeek = transfersByGameWeek;
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
                await Publish(async slackClient =>
                {
                    var player = players.Single(x => x.Id == key);
                    var goals = newGoalsByPlayer[key];
                    var message = $"{player.FirstName} {player.SecondName} just scored {(goals == 1 ? "a goal" : $"{goals} goals")}!";

                    var users = (await slackClient.UsersList())?.Members?.Where(user => user.IsActiveRealPerson()).ToArray();
                    if (users == null) return message;

                    var entriesTransferredPlayerOut = EntriesThatTransferredPlayerOutThisGameweek(users, player.Id).ToArray();
                    if (entriesTransferredPlayerOut.Any())
                    {
                        message += $" Lol, you transferred him out, {string.Join(", ", entriesTransferredPlayerOut)} :joy:";
                    }

                    return message;
                });
            }
        }

        private IEnumerable<string> EntriesThatTransferredPlayerOutThisGameweek(User[] users, int playerId)
        {
            return _transfersForCurrentGameweek == null ? 
                Enumerable.Empty<string>() : 
                _transfersForCurrentGameweek.Where(x => x.PlayerTransferredOut == playerId).Select(x => GetSlackHandle(users, x.EntryName) ?? x.EntryName);
        }

        private static string GetSlackHandle(User[] users, string entryName)
        {
            return SearchForHandle(users, user => user.Real_name, entryName) ??
                   SearchForHandle(users, user => user.Real_name?.Split(" ")?.First(), entryName?.Split(" ")?.First()) ??
                   SearchForHandle(users, user => user.Real_name?.Split(" ")?.Last(), entryName?.Split(" ")?.Last());
        }

        private static string SearchForHandle(IEnumerable<User> users, Func<User, string> userProp, string searchFor)
        {
            var matches = users.Where(user => Equals(searchFor, userProp(user))).ToArray();
            if (matches.Length > 1)
            {
                // if more than one slack user has a name that matches the fpl entry,
                // we shouldn't @ either one of them, cause we're not sure who's the right one
                return null;
            }

            return matches.Length == 1 ? GetSlackHandle(matches.Single()) : null;
        }

        private static bool Equals(string s1, string s2)
        {
            if (s1 == null || s2 == null)
            {
                return false;
            }
            return string.Equals(s1, s2, StringComparison.CurrentCultureIgnoreCase);
        }

        private static string GetSlackHandle(User user) => $"@{user.Name}";

        public override string Cron => Constants.CronPatterns.EveryMinute;
    }
}