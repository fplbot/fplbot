using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers;
using Slackbot.Net.Extensions.FplBot.Models;
using Slackbot.Net.Extensions.FplBot.Taunts;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal class GameweekEventsFormatter
    {

        public static List<string> FormatNewFixtureEvents(List<FixtureEvents> newFixtureEvents, GameweekLeagueContext context)
        {
            var formattedStrings = new List<string>();

            newFixtureEvents.ForEach(newFixtureEvent =>
            {
                var scoreHeading = $"{GetScore(context.Teams, newFixtureEvent)}\n";
                var eventMessages = newFixtureEvent.StatMap
                    .Where(stat => TeamSubscribesForThisEvent(context, stat.Key))
                    .SelectMany(stat =>
                {
                    switch (stat.Key)
                    {
                        case StatType.GoalsScored:
                            return FormatNewGoals(stat.Value, context);
                        case StatType.Assists:
                            return FormatNewAssists(stat.Value, context);
                        case StatType.OwnGoals:
                            return FormatOwnGoals(stat.Value, context);
                        case StatType.RedCards:
                            return FormatNewRedCards(stat.Value, context);
                        case StatType.PenaltiesMissed:
                            return FormatNewPenaltiesMissed(stat.Value, context);
                        case StatType.PenaltiesSaved:
                            return FormatNewPenaltiesSaved(stat.Value, context);
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

        private static bool TeamSubscribesForThisEvent(GameweekLeagueContext context, StatType statType)
        {
            return context.EventSubscriptions.ContainsSubscriptionFor(statType.GetSubscriptionType());
        }

        private static IEnumerable<object> FormatNewAssists(
            List<PlayerEvent> newAssistEvents,
            GameweekLeagueContext context)
        {
            return FormatEvent(
                newAssistEvents,
                context,
                "got an assist!",
                ":handshake:",
                new AssistTaunt());
        }

        private static IEnumerable<object> FormatOwnGoals(
            List<PlayerEvent> newOwnGoalEvents,
            GameweekLeagueContext context)
        {
            return FormatEvent(
                newOwnGoalEvents,
                context,
                "scored a goal! In his own goal!",
                ":face_palm:",
                new OwnGoalTaunt());
        }

        private static IEnumerable<object> FormatNewPenaltiesMissed(
            List<PlayerEvent> newPenaltiesMissedEvents,
            GameweekLeagueContext context)
        {
            return FormatEvent(
                newPenaltiesMissedEvents,
                context,
                "missed a penalty!",
                ":dizzy_face:",
                new PenaltyMissTaunt());
        }

        private static IEnumerable<object> FormatNewPenaltiesSaved(
            List<PlayerEvent> newPenaltiesSavedEvents,
            GameweekLeagueContext context)
        {
            return FormatEvent(
                newPenaltiesSavedEvents,
                context,
                "saved a penalty!",
                ":man-cartwheeling:",
                null);
        }

        private static List<string> FormatNewGoals(
            List<PlayerEvent> newGoalEvents,
            GameweekLeagueContext context)
        {
            return FormatEvent(
                newGoalEvents,
                context,
                "scored a goal!",
                ":soccer:",
                new GoalTaunt());
        }

        private static List<string> FormatNewRedCards(
            List<PlayerEvent> newRedCardEvents,
            GameweekLeagueContext context)
        {
            return FormatEvent(
                newRedCardEvents,
                context,
                "got a red card!",
                ":red_circle:",
                new RedCardTaunt());
        }

        private static List<string> FormatEvent(
            List<PlayerEvent> newGoalEvents,
            GameweekLeagueContext context,
            string eventDescription,
            string eventEmoji,
            ITaunt taunt)
        {
            return newGoalEvents.Select(g =>
            {
                var player = context.Players.Single(x => x.Id == g.PlayerId);
                var message = $"{player.FirstName} {player.SecondName} {eventDescription} {eventEmoji} ";

                if (g.IsRemoved)
                {
                    message = $"~{message.TrimEnd()}~ (VAR? :shrug:)";
                }
                else if (taunt != null && context.EventSubscriptions.ContainsSubscriptionFor(EventSubscription.Taunts))
                {
                    var tauntibleEntries = GetTauntibleEntries(context, player, taunt.Type);
                    var append = tauntibleEntries.Any() ? $" {string.Format(taunt.JokePool.GetRandom(), string.Join(", ", tauntibleEntries))}" : null;
                    message += append;
                }

                return message;

            }).WhereNotNull().ToList();
        }

        private static string[] GetTauntibleEntries(GameweekLeagueContext context, Player player, TauntType tauntType)
        {
            switch (tauntType)
            {
                case TauntType.HasPlayerInTeam:
                    return EntriesThatHasPlayerInTeam(player.Id, context.GameweekEntries, context.Users).ToArray();
                case TauntType.InTransfers:
                    return EntriesThatTransferredPlayerInThisGameweek(player.Id, context.TransfersForLeague, context.Users).ToArray();
                case TauntType.OutTransfers:
                    return EntriesThatTransferredPlayerOutThisGameweek(player.Id, context.TransfersForLeague, context.Users).ToArray();
                default:
                    return Array.Empty<string>();
            }
        }

        private static string GetScore(ICollection<Team> teams, FixtureEvents fixtureEvent)
        {
            var gameScore = fixtureEvent.GameScore;
            return $"{teams.Single(team => team.Id == gameScore.HomeTeamId).Name} " +
                   $"{gameScore.HomeTeamScore}-{gameScore.AwayTeamScore} " +
                   $"{teams.Single(team => team.Id == gameScore.AwayTeamId).Name}";
        }

        private static IEnumerable<string> EntriesThatTransferredPlayerOutThisGameweek(int playerId, IEnumerable<TransfersByGameWeek.Transfer> transfersForCurrentGameweek, IEnumerable<User> users)
        {
            return transfersForCurrentGameweek == null ? 
                Enumerable.Empty<string>() : 
                transfersForCurrentGameweek.Where(x => x.PlayerTransferredOut == playerId).Select(x => SlackHandleHelper.GetSlackHandleOrFallback(users, x.EntryName));
        }

        private static IEnumerable<string> EntriesThatTransferredPlayerInThisGameweek(int playerId, IEnumerable<TransfersByGameWeek.Transfer> transfersForCurrentGameweek, IEnumerable<User> users)
        {
            return transfersForCurrentGameweek == null ?
                Enumerable.Empty<string>() :
                transfersForCurrentGameweek.Where(x => x.PlayerTransferredIn == playerId).Select(x => SlackHandleHelper.GetSlackHandleOrFallback(users, x.EntryName));
        }

        private static IEnumerable<string> EntriesThatHasPlayerInTeam(int playerId, IEnumerable<GameweekEntry> gameweekEntries, IEnumerable<User> users)
        {
            return gameweekEntries == null ?
                Enumerable.Empty<string>() :
                gameweekEntries.Where(x => x.Picks.Any(pick => pick.PlayerId == playerId)).Select(x => SlackHandleHelper.GetSlackHandleOrFallback(users, x.EntryName));
        }
    }

}