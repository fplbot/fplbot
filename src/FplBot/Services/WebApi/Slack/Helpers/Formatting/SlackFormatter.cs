using Fpl.Client.Models;
using FplBot.Formatting;
using Slackbot.Net.Models.BlockKit;

namespace FplBot.Services.WebApi.Slack.Helpers.Formatting;

public static class SlackFormatter
{
    public static IBlock[] GetPlayerCard(Player player, ICollection<Team> teams, string imageUrl)
    {
        List<IBlock> playerCard =
        [
            new SectionBlock { text = new Text { type = "mrkdwn", text = $"*{player.FirstName} {player.SecondName}*" } }
        ];


        playerCard.Add(new ImageBlock
        {
            image_url = imageUrl,
            title = new Text { text = $"{player.SecondName}.png" },
            alt_text = $"{player.FirstName} {player.SecondName}"
        });

        var team = teams.FirstOrDefault(t => t.Code == player.TeamCode);
        var teamName = team != null ? team.Name : "";

        Text[] fields =
        [
            new Text { type = "mrkdwn", text = $"*Team*: {teamName}" },
            new Text { type = "mrkdwn", text = $"*Points*: {player.TotalPoints}" },
            new Text { type = "mrkdwn", text = $"*Cost*: {Formatter.FormatCurrency(player.NowCost)}" },
            new Text { type = "mrkdwn", text = $"*Goals*: {player.GoalsScored}" },
            new Text { type = "mrkdwn", text = $"*Assists*: {player.Assists}" }
        ];

        playerCard.Add(new SectionBlock { fields = fields });

        playerCard.Add(new DividerBlock());

        var chanceOfPlaying = GetChanceOfPlayingWarningIfRelevant(player.ChanceOfPlayingNextRound, player.News);
        if (chanceOfPlaying != null)
        {
            playerCard.Add(new SectionBlock { text = new Text { type = "mrkdwn", text = chanceOfPlaying } });
        }

        return [.. playerCard];
    }

    private static string? GetChanceOfPlayingWarningIfRelevant(int? chanceOfPlaying, string? news)
    {
        if (!chanceOfPlaying.HasValue || chanceOfPlaying.Value == 100)
        {
            return null;
        }

        var text = news == "" ? $"Chance of playing next round: {chanceOfPlaying}%" : news;
        return $":warning: {text} \n";
    }
}
