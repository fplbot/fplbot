using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fpl.Client.Models;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal class Formatter
    {
        public static string GetStandings(ClassicLeague league, ICollection<Gameweek> gameweeks)
        {
            var sb = new StringBuilder();

            var sortedByRank = league.Standings.Entries.OrderBy(x => x.Rank);

            var numPlayers = league.Standings.Entries.Count();

            var currentGw = gameweeks.SingleOrDefault(x => x.IsCurrent)?.Id.ToString() ?? "?";

            
            sb.Append($":star: *Results after GW {currentGw}* :star: \n\n");

            foreach (var player in sortedByRank)
            {
                var arrow = GetRankChangeEmoji(player, numPlayers);
                sb.Append($"{player.Rank}. <https://fantasy.premierleague.com/entry/{player.Id}/event/{currentGw}|{player.EntryName}> - {player.Total} {arrow} \n");
            }

            return sb.ToString();
        }

        private static string GetRankChangeEmoji(ClassicLeagueEntry player, int numPlayers)
        {
            var rankDiff = player.LastRank - player.Rank;

            var emojiString = new StringBuilder();

            if (rankDiff < 0)
            {
                emojiString.Append($":chart_with_downwards_trend: ({rankDiff}) ");
            }

            if (rankDiff > 0)
            {
                emojiString.Append($":chart_with_upwards_trend: (+{rankDiff}) ");
            }

            if (player.Rank == numPlayers)
            {
                emojiString.Append(":rip:");
            }

            return emojiString.ToString();
        }

        public static string GetPlayer(Player player, ICollection<Team> teams)
        {
            var sb = new StringBuilder();

            sb.Append($":male_mage: *{player.FirstName} {player.SecondName}*\n");

            var team = teams.FirstOrDefault(t => t.Code == player.TeamCode);

            if (team != null)
            {
                sb.Append(GetTeamData(team));
            }

            sb.Append($"Points: {player.TotalPoints}\n");

            var costAsString = player.NowCost.ToString();
            sb.Append($"Cost: {costAsString.Insert(costAsString.Length - 1, ".")}\n");

            sb.Append($"Goals: {player.GoalsScored}\n");

            sb.Append($"Assists: {player.Assists}\n");


            sb.Append(GetChanceOfPlayingWarningIfRelevant(player.ChanceOfPlayingNextRound, player.News));

            return sb.ToString();
        }

        public static string GetTeamData(Team team)
        {
            return $"Team: {team.Name}\n";
        }

        public static string GetChanceOfPlayingWarningIfRelevant(string chanceOfPlaying, string news)
        {
            if (chanceOfPlaying == "100" || chanceOfPlaying == null)
            {
                return "";
            }
            else
            {
                var text = news == "" ? $"Chance of playing next round: {chanceOfPlaying}%" : news;
                return $":warning: {text} \n";
            }
        }

        public static string GetInjuredPlayers(IEnumerable<Player> players)
        {
            var sb = new StringBuilder();

            sb.Append($":helmet_with_white_cross: *Injured players*\n");

            foreach (var player in players)
            {
                var text = player.News == "" ? $"Chance of playing next round: {player.ChanceOfPlayingNextRound}%" : player.News;
                sb.Append($"*{player.FirstName} {player.SecondName}* - {text} (_Owned by {player.OwnershipPercentage}%_)\n");
            }

            return sb.ToString();
        }
    }
}
