using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal class GameweekEventsFormatter
    {
        private static readonly string[] TransferredGoalScorerOutTaunts =
        {
            "Ah jiiz, you transferred him out, {0} :joy:",
            "You just had to knee jerk him out, didn't you, {0}?",
            "Didn't you have that guy last week, {0}?",
            "Goddammit, really? You couldn't hold on to him just one more gameweek, {0}?"
        };

        private static readonly string[] GoodTransferMessages =
        {
            "Nice save {0} ! :v:",
            "How did you know, {0}?",
            "Trying to climb the ranks, {0}? :chart_with_upwards_trend:"
        };

        public static List<string> FormatNewFixtureEvents(
            List<FixtureEvents> newFixtureEvents, GameweekLeagueContext context)
        {
            var formattedStrings = new List<string>();

            newFixtureEvents.ForEach(newFixtureEvent =>
            {
                var scoreHeading = $"{GetScore(context.Teams, newFixtureEvent)}\n";
                var eventMessages = newFixtureEvent.StatMap.SelectMany(stat =>
                {
                    switch (stat.Key)
                    {
                        case StatType.GoalsScored:
                            return FormatNewGoals(stat.Value, context.Players, context.TransfersForLeague);
                        case StatType.Assists:
                            return FormatNewAssists(stat.Value, context.Players);
                        case StatType.OwnGoals:
                            return FormatOwnGoals(stat.Value, context.Players);
                        case StatType.RedCards:
                            return FormatNewRedCards(stat.Value, context.Players, context.TransfersForLeague);
                        case StatType.PenaltiesMissed:
                            return FormatNewPenaltiesMissed(stat.Value, context.Players);
                        case StatType.PenaltiesSaved:
                            return FormatNewPenaltiesSaved(stat.Value, context.Players);
                        default: return Enumerable.Empty<string>();
                    }
                }).MaterializeToArray();
                if (eventMessages.Any())
                {
                    formattedStrings.Add(scoreHeading + string.Join("\n", eventMessages.Select(s => $":black_small_square: {s}")));
                }
            });

            return formattedStrings;
        }

        private static IEnumerable<object> FormatNewAssists(
            List<PlayerEvent> newAssistEvents, 
            ICollection<Player> players)
        {
            return FormatEvent(
                newAssistEvents,
                players,
                "got an assist!",
                ":handshake:");
        }

        private static IEnumerable<object> FormatOwnGoals(
            List<PlayerEvent> newOwnGoalEvents,
            ICollection<Player> players)
        {
            return FormatEvent(
                newOwnGoalEvents,
                players,
                "scored a goal! In his own goal!",
                ":face_palm:");
        }

        private static IEnumerable<object> FormatNewPenaltiesMissed(
            List<PlayerEvent> newPenaltiesMissedEvents,
            ICollection<Player> players)
        {
            return FormatEvent(
                newPenaltiesMissedEvents,
                players,
                "missed a penalty!",
                ":dizzy_face:");
        }

        private static IEnumerable<object> FormatNewPenaltiesSaved(
            List<PlayerEvent> newPenaltiesSavedEvents,
            ICollection<Player> players)
        {
            return FormatEvent(
                newPenaltiesSavedEvents,
                players,
                "saved a penalty!",
                ":man-cartwheeling:");
        }

        private static List<string> FormatNewGoals(
            List<PlayerEvent> newGoalEvents, 
            ICollection<Player> players, 
            IEnumerable<TransfersByGameWeek.Transfer> transfersForCurrentGameweek)
        {
            return FormatEvent(
                newGoalEvents,
                players,
                "scored a goal!",
                ":soccer:",
                player =>
                {
                    var entriesTransferredPlayerOut = EntriesThatTransferredPlayerOutThisGameweek(player.Id, transfersForCurrentGameweek).ToArray();
                    return entriesTransferredPlayerOut.Any() ? $" {string.Format(TransferredGoalScorerOutTaunts.GetRandom(), string.Join(", ", entriesTransferredPlayerOut))}" : null;
                });
        }

        private static List<string> FormatNewRedCards(
            List<PlayerEvent> newRedCardEvents,
            ICollection<Player> players,
            IEnumerable<TransfersByGameWeek.Transfer> transfersForCurrentGameweek)
        {
            return FormatEvent(
                newRedCardEvents,
                players,
                "got a red card!",
                ":red_circle:",
                player =>
                {
                    var entriesTransferredPlayerOut = EntriesThatTransferredPlayerOutThisGameweek(player.Id, transfersForCurrentGameweek).ToArray();
                    return entriesTransferredPlayerOut.Any() ? $" {string.Format(GoodTransferMessages.GetRandom(), string.Join(", ", entriesTransferredPlayerOut))}" : null;
                });
        }

        private static List<string> FormatEvent(
            List<PlayerEvent> newGoalEvents,
            ICollection<Player> players,
            string eventDescription,
            string eventEmoji,
            Func<Player, string> append = null)
        {
            return newGoalEvents.Select(g =>
            {
                var player = players.Single(x => x.Id == g.PlayerId);
                var message = $"{player.FirstName} {player.SecondName} {eventDescription} {eventEmoji} ";

                if (g.IsRemoved)
                {
                    message = $"~{message}~ (VAR? :shrug:)";
                }
                else
                {
                    message += append?.Invoke(player);
                }

                return message;

            }).WhereNotNull().ToList();
        }

        private static string GetScore(ICollection<Team> teams, FixtureEvents fixtureEvent)
        {
            var gameScore = fixtureEvent.GameScore;
            return $"{teams.Single(team => team.Id == gameScore.HomeTeamId).Name} " +
                   $"{gameScore.HomeTeamScore}-{gameScore.AwayTeamScore} " +
                   $"{teams.Single(team => team.Id == gameScore.AwayTeamId).Name}";
        }

        private static IEnumerable<string> EntriesThatTransferredPlayerOutThisGameweek(int playerId, IEnumerable<TransfersByGameWeek.Transfer> transfersForCurrentGameweek)
        {
            return transfersForCurrentGameweek == null ? 
                Enumerable.Empty<string>() : 
                transfersForCurrentGameweek.Where(x => x.PlayerTransferredOut == playerId).Select(x => x.EntryName);
        }
    }
}