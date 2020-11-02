using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage.Blocks;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal static class Formatter
    {
        public static string GetStandings(ClassicLeague league, ICollection<Gameweek> gameweeks)
        {
            var sb = new StringBuilder();
         
            var sortedByRank = league.Standings.Entries.OrderBy(x => x.Rank);

            var numPlayers = league.Standings.Entries.Count;

            var currentGw = gameweeks.SingleOrDefault(x => x.IsCurrent);

            if (currentGw == null)
            {
                sb.Append("No current gameweek!");
                return sb.ToString();
            }

            sb.Append($":star: *Standings for {currentGw.Name}* :star: \n\n");

            foreach (var player in sortedByRank)
            {
                var arrow = GetRankChangeEmoji(player, numPlayers);
                sb.Append($"{player.Rank}. {player.GetEntryLink(currentGw.Id)} - {player.Total} {arrow} \n");
            }

            return sb.ToString();
        }

        private static string GetRankChangeEmoji(ClassicLeagueEntry player, int numPlayers)
        {
            if (player.LastRank == 0)
            {
                return ":wave: (joined this gameweek)";
            }

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
                emojiString.Append(":hankey:");
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

            sb.Append($"Cost: {FormatCurrency(player.NowCost)}\n");

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
            if (!players.Any())
                return "No injuries amongst the most selected players";

            var sb = new StringBuilder();

            sb.Append($":helmet_with_white_cross: *Injured players*\n");

            foreach (var player in players)
            {
                var text = player.News == "" ? $"Chance of playing next round: {player.ChanceOfPlayingNextRound}%" : player.News;
                sb.Append($"*{player.FirstName} {player.SecondName}* - {text} (_Owned by {player.OwnershipPercentage}%_)\n");
            }

            return sb.ToString();
        }

        public static IBlock[] GetPlayerCard(Player player, ICollection<Team> teams)
        {

            List<IBlock> playerCard = new List<IBlock>();

            playerCard.Add(new SectionBlock
            {
                text = new Text
                {
                    type = "mrkdwn",
                    text = $"*{player.FirstName} {player.SecondName}*"
                }
            });


            var imageUrl = $"https://platform-static-files.s3.amazonaws.com/premierleague/photos/players/110x140/p{player.Code}.png";

            if (!ImageIsAvailable(imageUrl))
                imageUrl = "https://user-images.githubusercontent.com/206726/73577018-207e4100-447c-11ea-98e3-9cc598c56519.png";

            playerCard.Add(new ImageBlock
            {
                image_url = imageUrl,
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
                    text = $"*Cost*: {FormatCurrency(player.NowCost)}"
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

        private static bool ImageIsAvailable(string imageUrl)
        {
            var httpClient = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Head, imageUrl);
            return httpClient.SendAsync(req).GetAwaiter().GetResult().IsSuccessStatusCode;
        }

        public static string FormatCurrency(int amount)
        {
            return (amount / 10.0).ToString("£0.0", CultureInfo.InvariantCulture);
        }

        public static string FormatPriceChanged(IEnumerable<Player> priceChangesPlayers, ICollection<Team> teams)
        {
            if (!priceChangesPlayers.Any())
                return "No players with price changes.";
            
            var messageToSend = "";
            var grouped = priceChangesPlayers.OrderByDescending(p => p.CostChangeEvent).ThenByDescending(p => p.NowCost).GroupBy(p => p.CostChangeEvent);
            foreach (var group in grouped)
            {
                var isPriceIncrease = @group.Key > 0;
                var priceChange = $"{FormatCurrency(group.Key)}";
                var header = isPriceIncrease ? $"*Price up {priceChange} :chart_with_upwards_trend:*" : $"*Price down {priceChange} :chart_with_downwards_trend:*";
                messageToSend += $"\n\n{header}";
                foreach (var p in group)
                {
                    var team = teams.FirstOrDefault(t => t.Code == p.TeamCode);
                    var teamName = team != null ? $"({team.Name})" : "";
                    messageToSend += $"\n• {p.FirstName} {p.SecondName} {teamName} {FormatCurrency(p.NowCost)}";
                }
            }

            return messageToSend;
        }

        public static string BulletPoints<T>(IEnumerable<T> list)
        {
            return string.Join("\n", list.Select(s => $":black_small_square: {s}"));
        }
    }

}
