using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Fpl.Client.Models;
using Fpl.Search.Models;
using FplBot.Core.Extensions;
using FplBot.Core.Models;
using FplBot.Data.Models;
using FplBot.Messaging.Contracts.Events.v1;
using Slackbot.Net.Models.BlockKit;

namespace FplBot.Core.Helpers
{
    public static class Formatter
    {
        public static string GetStandings(ClassicLeague league, Gameweek gameweek)
        {
            var sb = new StringBuilder();

            var sortedByRank = league.Standings.Entries.OrderBy(x => x.Rank);

            var numPlayers = league.Standings.Entries.Count;

            if (gameweek == null)
            {
                sb.Append("No current gameweek!");
                return sb.ToString();
            }

            sb.Append($":star: *Here's the current standings after {gameweek.Name}* :star: \n\n");

            foreach (var player in sortedByRank)
            {
                var arrow = GetRankChangeEmoji(player, numPlayers, gameweek.Id);
                sb.Append($"{player.Rank}. {player.GetEntryLink(gameweek.Id)} - {player.Total} {arrow} \n");
            }

            return sb.ToString();
        }

        public static string GetTopThreeGameweekEntries(ClassicLeague league, Gameweek gameweek)
        {
            var topThree = league.Standings.Entries
                .GroupBy(e => e.EventTotal)
                .OrderByDescending(g => g.Key)
                .Take(3)
                .ToArray();

            if (!topThree.Any())
            {
                return null;
            }

            var sb = new StringBuilder();

            sb.Append("Top three this gameweek was:\n");

            for (var i = 0; i < topThree.Length; i++)
            {
                var group = topThree[i];
                foreach (var entry in group)
                {
                    sb.Append($"{Formatter.RankEmoji(i)} {entry.GetEntryLink(gameweek.Id)} - {entry.EventTotal}\n");
                }
            }

            return sb.ToString();
        }

        public static string GetWorstGameweekEntry(ClassicLeague league, Gameweek gameweek)
        {
            var worst = league.Standings.Entries.OrderBy(e => e.EventTotal).FirstOrDefault();
            return worst == null ? null : $":poop: {worst.GetEntryLink(gameweek.Id)} only got {worst.EventTotal} points. Wow.";
        }

        private static string GetRankChangeEmoji(ClassicLeagueEntry player, int numPlayers, int gameweekId)
        {
            if (gameweekId == 1)
                return string.Empty;

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

        public static string FormatNewPlayers(IEnumerable<NewPlayer> newPlayers)
        {
            var header = $"🆕 New player!";
            if (newPlayers.Count() > 1)
            {
                header = "🆕 New players!";
            }
            var nameAndCost = newPlayers.Select(NameAndCost);
            return $"{header}\n{BulletPoints(nameAndCost)}";

            string NameAndCost(NewPlayer p)
            {
                return $"{p.WebName} ({p.TeamShortName}) {FormatCurrency(p.NowCost)}";
            }
        }

        public static string FormatPriceChanged(IEnumerable<PlayerWithPriceChange> priceChangesPlayers)
        {
            if (!priceChangesPlayers.Any())
                return "No players with price changes.";

            var messageToSend = "";
            var grouped = priceChangesPlayers.OrderByDescending(p => p.CostChangeEvent).ThenByDescending(p => p.NowCost).GroupBy(p => p.CostChangeEvent);
            foreach (var group in grouped)
            {
                var priceChange = $"{FormatCurrency(group.Key)}";
                var header = @group.Key switch
                {
                    var p when p > 0 => $"*Price up {priceChange} 📈*",
                    var p when p < 0 => $"*Price down {priceChange} 📉*",
                    var p when p == 0 => $"*Back to status quo… 🙃*",
                    _ => "*No idea*"
                };
                messageToSend += $"\n\n{header}";
                foreach (var p in group)
                {
                    messageToSend += $"\n• {p.FirstName} {p.SecondName} ({p.TeamShortName}) {FormatCurrency(p.NowCost)}";
                }
            }

            return messageToSend;
        }

        public static string BulletPoints<T>(IEnumerable<T> list)
        {
            return string.Join("\n", list.Select(s => $":black_small_square: {s}"));
        }

        public static string FormatInjuryStatusUpdates(IEnumerable<InjuredPlayerUpdate> statusUpdates)
        {
            var grouped = statusUpdates.GroupBy(Change).Where(c => c.Key != null);
            var sb = new StringBuilder();
            foreach (var group in grouped)
            {
                sb.Append($"*{group.Key}*\n");
                foreach (var gUpdate in group)
                {
                    var chance = string.Empty;
                    var chanceOfPlayingChange = ChanceOfPlayingChange(gUpdate);
                    if (chanceOfPlayingChange.HasValue && chanceOfPlayingChange != 0)
                    {
                        chance += chanceOfPlayingChange > 0 ? $"[+" : "[";
                        chance += $"{chanceOfPlayingChange}%]";
                    }

                    sb.Append($"• {gUpdate.Player.WebName} ({gUpdate.Player.Team.ShortName}). {gUpdate.Updated.News} {chance}\n");
                }
            }
            return sb.ToString();
        }

        public static string Change(InjuredPlayerUpdate update)
        {
            return (PreviousStatus: update.Previous, UpdatedStatus: update.Updated) switch
            {
                (null, null) => null,
                (null,_) => null,
                (_, null) => null,
                (_,_) when update.Previous == update.Updated => null,
                (_,_) => (update.Previous.Status, update.Updated.Status) switch
                {
                    (PlayerStatuses.Doubtful,PlayerStatuses.Doubtful) when ChanceOfPlayingChange(update) > 0 => "📈️ Increased chance of playing",
                    (PlayerStatuses.Doubtful,PlayerStatuses.Doubtful) when ChanceOfPlayingChange(update) < 0 => "📉️ Decreased chance of playing",
                    (PlayerStatuses.Doubtful,PlayerStatuses.Doubtful) when NewsAdded(update) => "ℹ️ News update",
                    (_, _) when update.Updated.Status.Contains("Self-isolating", StringComparison.InvariantCultureIgnoreCase) => "🦇 COVID-19 🦇",
                    (_, PlayerStatuses.Injured) => "🤕 Injured",
                    (_, PlayerStatuses.Doubtful) => "⚠️ Doubtful",
                    (_, PlayerStatuses.Suspended) => "❌ Suspended",
                    (_, PlayerStatuses.Unavailable) => "👀 Unavailable",
                    (_, PlayerStatuses.NotInSquad) => "😐 Not in squad",
                    (_, PlayerStatuses.Available) => "✅ Available",
                    (_, _) => null
                }
            };
        }

        private static bool NewsAdded(InjuredPlayerUpdate playerStatusUpdate)
        {
            return playerStatusUpdate.Previous.News == null && playerStatusUpdate.Updated.News != null;
        }

        private const string ChanceOfPlayingPattern = "(\\d+)\\% chance of playing";

        private static int? ChanceOfPlayingChange(InjuredPlayerUpdate playerStatusUpdate)
        {
            if (playerStatusUpdate.Previous?.News != null && playerStatusUpdate.Updated.News != null)
            {
                var fromChanceMatch = Regex.Matches(playerStatusUpdate.Previous.News, ChanceOfPlayingPattern, RegexOptions.IgnoreCase);
                var toChanceMatch = Regex.Matches(playerStatusUpdate.Updated.News, ChanceOfPlayingPattern, RegexOptions.IgnoreCase);
                if (fromChanceMatch.Any() && toChanceMatch.Any())
                {
                    var fromChance = int.Parse(fromChanceMatch.First().Groups[1].Value);
                    var toChance = int.Parse(toChanceMatch.First().Groups[1].Value);
                    return toChance - fromChance;
                }
            }
            return null;
        }

        private static int? ChanceOfPlayingChange(PlayerUpdate playerStatusUpdate)
        {
            if (playerStatusUpdate.FromPlayer?.News != null && playerStatusUpdate.ToPlayer.News != null)
            {
                var fromChanceMatch = Regex.Matches(playerStatusUpdate.FromPlayer.News, ChanceOfPlayingPattern, RegexOptions.IgnoreCase);
                var toChanceMatch = Regex.Matches(playerStatusUpdate.ToPlayer.News, ChanceOfPlayingPattern, RegexOptions.IgnoreCase);
                if (fromChanceMatch.Any() && toChanceMatch.Any())
                {
                    var fromChance = int.Parse(fromChanceMatch.First().Groups[1].Value);
                    var toChance = int.Parse(toChanceMatch.First().Groups[1].Value);
                    return toChance - fromChance;
                }
            }
            return null;
        }

        public static string FormatLineup(Lineups details)
        {
            var formattedOutput = "";
            FormatTeamLineup(details.HomeTeamLineup, ref formattedOutput);
            FormatTeamLineup(details.AwayTeamLineup, ref formattedOutput, true);
            return formattedOutput;
        }

        private static void FormatTeamLineup(FormationDetails playerInLineup, ref string formattedOutput, bool reverse = false)
        {
            formattedOutput += $"*{playerInLineup.TeamName}* ({playerInLineup.Formation})\n";
            var formationSegments = playerInLineup.Segments;
            if (reverse)
                formationSegments.Reverse();

            foreach (var segment in formationSegments)
            {
                formattedOutput += $"{PositionEmoji(segment.SegmentPosition)}  ";
                var segmentPlayersInSegment = segment.PlayersInSegment;
                var playersInSegment = segmentPlayersInSegment.Select(player => player.Captain ? $"{player.Name}©" : $"{player.Name}");

                if (reverse)
                {
                    playersInSegment = playersInSegment.Reverse();
                }
                formattedOutput += $"{string.Join("  ", playersInSegment)}\n";
            }
            formattedOutput += "\n";
        }

        private static string PositionEmoji(string position)
        {
            return position switch
            {
                PlayerInLineup.MatchPositionGoalie => "🧤",
                PlayerInLineup.MatchPositionDefender => "🛡",
                PlayerInLineup.MatchPositionMidfielder => "⚙️",
                PlayerInLineup.MatchPositionForward => "⚡️️",
                _ => "⁇"
            };
        }

        public static string FormatProvisionalFinished(FinishedFixture fixture)
        {
            var fullTimeReport = "";
            if (fixture.BonusPoints.Any())
            {
                var bonusPointsOutput = CreateBonusPointsOutput(fixture);
                fullTimeReport += $"\nBonus points:\n";
                fullTimeReport += BulletPoints(bonusPointsOutput);
            }
            return fullTimeReport;
        }

        static string BonusPointRank(int bonusPoints, IEnumerable<Player> pall)
        {
            return $"{bonusPoints}p {string.Join(", ", pall.OrderBy(p => p.WebName).Select(p => p.WebName))}";
        }

        //https://www.premierleague.com/news/106533
        //The players with the top three BPS in a given match receive bonus points
        // - three points to the highest-scoring player,
        // - two to the second best and
        // - one to the third.
        //
        // Bonus point ties are resolved as follows:
        // - If there is a tie for first place, Players 1 & 2 will receive 3 points each and Player 3 will receive 1 point.
        // - If there is a tie for second place, Player 1 will receive 3 points and Players 2 and 3 will receive 2 points each.
        // - If there is a tie for third place, Player 1 will receive 3 points, Player 2 will receive 2 points and Players 3 & 4 will receive 1 point each.
        public static IEnumerable<string> CreateBonusPointsOutput(FinishedFixture fixture)
        {
            var bonusPointsOutput = new List<string>();
            var pallenCount = 0;
            var groupedByBonusPoints = fixture.BonusPoints.OrderByDescending(b => b.BonusPoints).GroupBy(bp => bp.BonusPoints);
            var points = 3;

            foreach (var bonusGroup in groupedByBonusPoints)
            {
                if (points == 3)
                {
                    if(bonusGroup.Count() >= 3)
                    {
                        bonusPointsOutput.Add(BonusPointRank(3, bonusGroup.Select(p => p.Player)));
                        break;
                    }
                    pallenCount = bonusGroup.Count();
                    bonusPointsOutput.Add(BonusPointRank(3, bonusGroup.Select(p => p.Player)));
                }

                if (points == 2)
                {
                    if (pallenCount > 1)
                    {
                        bonusPointsOutput.Add(BonusPointRank(1, bonusGroup.Select(p => p.Player)));
                    }
                    else
                    {
                        bonusPointsOutput.Add(BonusPointRank(2, bonusGroup.Select(p => p.Player)));
                    }

                    pallenCount += bonusGroup.Count();
                }

                if (points == 1)
                {
                    if (pallenCount == 2)
                    {
                        bonusPointsOutput.Add(BonusPointRank(1, bonusGroup.Select(p => p.Player)));
                    }
                }


                points--;

                if (points == 0)
                    break;
            }
            return bonusPointsOutput;
        }

        public static string FormatGameweekFinished(Gameweek gw, ClassicLeague league)
        {
            var introText = $"{gw.Name} is finished.";
            var globalAverage = (int)Math.Round(gw.AverageScore);
            var leagueAverage = (int)Math.Round(league.Standings.Entries.Average(entry => entry.EventTotal));
            var diff = Math.Abs(globalAverage - leagueAverage);
            var nuance = diff <= 5 ? "slightly " : "";

            if (globalAverage < 40)
            {
                introText += $" It was probably a disappointing one, with a global average of *{gw.AverageScore}* points.";
            }
            else if (globalAverage > 80)
            {
                introText += $" Must've been pretty intense, with a global average of *{globalAverage}* points.";
            }
            else
            {
                introText += $" The global average was *{globalAverage}* points.";
            }

            if (leagueAverage > globalAverage)
            {
                introText += $" Your league did {nuance}better than this, though - with *{leagueAverage}* points average.";
            }
            else if (leagueAverage == globalAverage)
            {
                introText += $" I guess your league is pretty mediocre, since you got the exact same *{leagueAverage}* points average.";
            }
            else
            {
                introText += $" I'm afraid your league did {nuance}worse than this, with your *{leagueAverage}* points average.";
            }

            return introText;
        }

        public static string RankEmoji(int position)
        {
            return position switch
            {
                0 => ":first_place_medal:",
                1 => ":second_place_medal:",
                2 => ":third_place_medal:",
                _ => FormattingConstants.Emojis.NatureEmojis.GetRandom()
            };
        }

        public static string FixturesForGameweek(int gwId, string name, DateTime deadline, ICollection<Fixture> fixtures, ICollection<Team> teams, int tzOffset)
        {
            var textToSend = $":information_source: <https://fantasy.premierleague.com/fixtures/{gwId}|{name.ToUpper()}>";
            textToSend += $"\nDeadline: {deadline.WithOffset(tzOffset):yyyy-MM-dd HH:mm}\n";

            var groupedByDay = fixtures.GroupBy(f => f.KickOffTime.Value.Date);

            foreach (var group in groupedByDay)
            {
                textToSend += $"\n{group.Key.WithOffset(tzOffset):dddd}";
                foreach (var fixture in group)
                {
                    var homeTeam = teams.First(t => t.Id == fixture.HomeTeamId);
                    var awayTeam = teams.First(t => t.Id == fixture.AwayTeamId);
                    var fixtureKickOffTime = fixture.KickOffTime.Value.WithOffset(tzOffset);
                    textToSend += $"\n•{fixtureKickOffTime:HH:mm} {homeTeam.ShortName}-{awayTeam.ShortName}";
                }

                textToSend += "\n";
            }

            return textToSend;
        }

        public static string FormatEntryItem(EntryItem entryItem, int? gameweek)
        {
            return GetEntryLink(entryItem.Id, entryItem.TeamName, gameweek) + $" ({entryItem.RealName})" + (entryItem.VerifiedType != null ? $" {GetVerifiedTypeEmoji(entryItem.VerifiedType)}" : null);
        }

        private static string GetVerifiedTypeEmoji(VerifiedEntryType? entryItemVerifiedType)
        {
            if (entryItemVerifiedType == null)
            {
                return null;
            }

            var emojis = ":ballot_box_with_check:";

            switch (entryItemVerifiedType)
            {
                case VerifiedEntryType.FootballerInPL:
                case VerifiedEntryType.Footballer:
                    emojis += ":soccer:";
                    break;
                case VerifiedEntryType.ChessMaster:
                    emojis += ":chess_pawn:";
                    break;
                case VerifiedEntryType.Podcaster:
                    emojis += ":studio_microphone:";
                    break;
                case VerifiedEntryType.CommunityFame:
                    emojis += ":bird:";
                    break;
                case VerifiedEntryType.Actor:
                    emojis += ":performing_arts:";
                    break;
                case VerifiedEntryType.TvFace:
                    emojis += ":tv:";
                    break;
                case VerifiedEntryType.Athlete:
                    emojis += ":sports_medal:";
                    break;
                case VerifiedEntryType.PastWinner:
                    emojis += ":trophy:";
                    break;
                default:
                    break;
            }

            return emojis;
        }

        public static string FormatLeagueItem(LeagueItem leagueItem, int? gameweek)
        {
            return GetLeagueLink(leagueItem.Id, leagueItem.Name) +
                   (leagueItem.AdminEntry.HasValue ?
                       $" (admin: {GetEntryLink(leagueItem.AdminEntry.Value, FormatLeagueAdmin(leagueItem.AdminName, leagueItem.AdminCountry), gameweek)})"
                       : null);
        }

        private static string FormatLeagueAdmin(string leagueAdminName, string leagueAdminCountryShortIso)
        {
            if (string.IsNullOrEmpty(leagueAdminName))
            {
                return "Go to admin";
            }

            return leagueAdminName + " " + FormatCountryShortIso(leagueAdminCountryShortIso);
        }

        public static string FormatCountryShortIso(string countryIso)
        {
            if (string.IsNullOrEmpty(countryIso))
            {
                return null;
            }

            countryIso = countryIso.ToLower();

            switch (countryIso)
            {
                case "en":
                    countryIso = "england";
                    break;
                case "s1":
                    countryIso = "scotland";
                    break;
                case "wa":
                    countryIso = "wales";
                    break;
            }

            return $":flag-{countryIso}:";
        }

        public static string GetEntryLink(int entryId, string name, int? gameweek)
        {
            return $"<https://fantasy.premierleague.com/entry/{entryId}/{GetLinkSuffix(gameweek)}|{name}>";
        }

        private static string GetLinkSuffix(int? gameweek) => gameweek.HasValue ? $"event/{gameweek.Value}" : "history";

        public static string GetLeagueLink(int leagueId, string name)
        {
            return $"<https://fantasy.premierleague.com/leagues/{leagueId}/standings/c|{name}>";
        }
    }
}
