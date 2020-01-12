using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fpl.Client.Models;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage.Blocks;

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
                sb.Append($"{player.Rank}. <https://fantasy.premierleague.com/entry/{player.Entry}/event/{currentGw}|{player.EntryName}> - {player.Total} {arrow} \n");
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

            var chanceOfPlaying = GetChanceOfPlayingWarningIfRelevant(player.ChanceOfPlayingNextRound, player.News);
            if (chanceOfPlaying != null)
            {
                sb.Append(chanceOfPlaying);
                sb.Append("\n");
            }

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
                return null;
            }
            var text = news == "" ? $"Chance of playing next round: {chanceOfPlaying}%" : news;
            return $":warning: {text} \n";
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

        public static IBlock[] GetPlayerCard(Player player, ICollection<Team> teams) {

            List<IBlock> playerCard = new List<IBlock>();

            playerCard.Add(new SectionBlock
            {
                text = new Text
                {
                    type = "mrkdwn",
                    text = $"*{player.FirstName} {player.SecondName}*"
                }
            });

            playerCard.Add(new ImageBlock
            {
                image_url = $"https://platform-static-files.s3.amazonaws.com/premierleague/photos/players/110x140/p{player.Code}.png",
                title = new Text
                {
                    text = $"{player.SecondName}.png"
                },
                alt_text = $"{player.FirstName} {player.SecondName}"
            });

            var team = teams.FirstOrDefault(t => t.Code == player.TeamCode);
            var teamName = team != null ? team.Name : "";

            Text[] fields =
            {
                new Text
                {
                    type = "mrkdwn",
                    text = $"*Team*: {teamName}"
                },
                new Text
                {
                    type = "mrkdwn",
                    text = $"*Points*: {player.TotalPoints}"
                },
                new Text
                {
                    type = "mrkdwn",
                    text = $"*Cost*: {player.NowCost / 10.0}"
                },
                new Text
                {
                    type = "mrkdwn",
                    text = $"*Goals*: {player.GoalsScored}"
                },
                new Text
                {
                    type = "mrkdwn",
                    text = $"*Assists*: {player.Assists}"
                }
            };

            playerCard.Add(new SectionBlock
            {
                fields = fields
            });

            playerCard.Add(new DividerBlock { });

            var chanceOfPlaying = GetChanceOfPlayingWarningIfRelevant(player.ChanceOfPlayingNextRound, player.News);
            if (chanceOfPlaying != null)
            {
                playerCard.Add(new SectionBlock
                {
                    text = new Text
                    {
                        type = "mrkdwn",
                        text = chanceOfPlaying
                    }
                });
            }

            return playerCard.ToArray();
        }
    }
}
