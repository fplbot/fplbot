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
        private readonly string[] _transferredGoalScorerOutTaunts =
        {
            "Ah jiiz, you transferred him out, {0} :joy:",
            "You just had to knee jerk him out, didn't you, {0}?",
            "Didn't you have that guy last week, {0}?",
            "Goddammit, really? You couldn't hold on to him just one more gameweek, {0}?"
        };


        public GoalMonitorRecurringAction(
            IOptions<FplbotOptions> options,
            IGameweekClient gwClient,
            ILogger<NextGameweekRecurringAction> logger,
            ITokenStore tokenStore,
            ISlackClientBuilder slackClientBuilder,
            IPlayerClient playerClient,
            IGoalsDuringGameweek goalsDuringGameweek,
            ITransfersByGameWeek transfersByGameWeek,
            IFetchFplbotSetup teamRepo) : 
            base(options, gwClient, logger, tokenStore, slackClientBuilder, teamRepo)
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
            _currentGoalsByPlayerDuringGameweek = await _goalsDuringGameweek.GetPlayerGoals(newGameweek);
            _transfersForCurrentGameweek = await _transfersByGameWeek.GetTransfersByGameweek(newGameweek, _options.Value.LeagueId);
        }

        protected override async Task DoStuffWithinCurrentGameweek(int currentGameweek, bool isFinished)
        {
            if (isFinished)
            {
                return;
            }

            var goalsByPlayer = await _goalsDuringGameweek.GetPlayerGoals(currentGameweek);
            var goalDiff = GoalDiff(_currentGoalsByPlayerDuringGameweek, goalsByPlayer);

            await ProcessNewGoals(goalDiff);

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
                    var message = $":soccer: {player.FirstName} {player.SecondName} just scored {(goals == 1 ? "a goal" : $"{goals} goals")}!";

                    var users = (await slackClient.UsersList())?.Members?.Where(user => user.IsActiveRealPerson()).ToArray();
                    if (users == null) return message;

                    var entriesTransferredPlayerOut = EntriesThatTransferredPlayerOutThisGameweek(users, player.Id).ToArray();
                    if (entriesTransferredPlayerOut.Any())
                    {
                        message += $" {string.Format(_transferredGoalScorerOutTaunts.GetRandom(), string.Join(", ", entriesTransferredPlayerOut))}";
                    }

                    return message;
                });
                await Task.Delay(2000);
            }
        }

        private IEnumerable<string> EntriesThatTransferredPlayerOutThisGameweek(User[] users, int playerId)
        {
            return _transfersForCurrentGameweek == null ? 
                Enumerable.Empty<string>() : 
                _transfersForCurrentGameweek.Where(x => x.PlayerTransferredOut == playerId).Select(x => SlackHandleHelper.GetSlackHandle(users, x.EntryName) ?? x.EntryName);
        }

        public override string Cron => Constants.CronPatterns.EveryMinute;
    }
}