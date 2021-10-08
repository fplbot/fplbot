using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Fpl.Client.Models;
using FplBot.Formatting;
using Slackbot.Net.Models.BlockKit;

namespace FplBot.Slack.Helpers.Formatting
{
    public static class SlackFormatter
    {
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
                    text = $"*Cost*: {Formatter.FormatCurrency(player.NowCost)}"
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

        private static string GetChanceOfPlayingWarningIfRelevant(int? chanceOfPlaying, string news)
        {
            if (!chanceOfPlaying.HasValue || chanceOfPlaying.Value == 100)
            {
                return null;
            }
            var text = news == "" ? $"Chance of playing next round: {chanceOfPlaying}%" : news;
            return $":warning: {text} \n";
        }

    }
}
