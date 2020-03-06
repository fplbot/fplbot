using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Options;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal class CaptainsByGameWeek : ICaptainsByGameWeek
    {
        private readonly IFetchFplbotSetup _fetcher;
        private readonly IEntryClient _entryClient;
        private readonly IPlayerClient _playerClient;
        private readonly ILeagueClient _leagueClient;
        private readonly IChipsPlayed _chipsPlayed;

        public CaptainsByGameWeek(IFetchFplbotSetup fetcher, IEntryClient entryClient, IPlayerClient playerClient, ILeagueClient leagueClient, IChipsPlayed chipsPlayed)
        {
            _fetcher = fetcher;
            _entryClient = entryClient;
            _playerClient = playerClient;
            _leagueClient = leagueClient;
            _chipsPlayed = chipsPlayed;
        }
        
        public async Task<string> GetCaptainsByGameWeek(int gameweek, int leagueId)
        {
            try
            {
                var entryCaptainPicks = await GetEntryCaptainPicks(gameweek, leagueId);
                
                var sb = new StringBuilder();
                sb.Append($":boom: *Captain picks for gameweek {gameweek}*\n");

                foreach (var entryCaptainPick in entryCaptainPicks)
                {
                    var captain = entryCaptainPick.Captain;
                    var viceCaptain = entryCaptainPick.ViceCaptain;

                    sb.Append($"*{entryCaptainPick.Entry.GetEntryLink(gameweek)}* - {captain.FirstName} {captain.SecondName} ({viceCaptain.FirstName} {viceCaptain.SecondName}) ");
                    if (entryCaptainPick.IsTripleCaptain)
                    {
                        sb.Append("TRIPLECAPPED!! :rocket::rocket::rocket::rocket:");
                    }

                    sb.Append("\n");
                }

                return sb.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return $"Oops: {e.Message}";
            }
        }

        public async Task<string> GetCaptainsChartByGameWeek(int gameweek, int leagueId)
        {
            try
            {
                var entryCaptainPicks = await GetEntryCaptainPicks(gameweek, leagueId);
                var captainGroups = entryCaptainPicks
                    .GroupBy(x => x.Captain.Id, el => el.Captain)
                    .OrderByDescending(x => x.Count())
                    .Select((group, i) => new { Captain = group.First(), Count = group.Count(), Emoji = GetCaptainCountEmoji(i) })
                    .MaterializeToArray();

                var sb = new StringBuilder();
                sb.Append($":bar_chart: *Captain picks chart for gameweek {gameweek}*\n\n");

                var max = captainGroups.Max(x => x.Count);

                for (var i = max; i > 0; i--)
                {
                    foreach (var captainGroup in captainGroups)
                    {
                        if (captainGroup.Count >= i)
                        {
                            sb.Append(":black_square:");
                        }
                        else
                        {
                            break;
                        }
                    }

                    sb.Append("\n");
                }

                foreach (var captainGroup in captainGroups)
                {
                    sb.Append($"{captainGroup.Emoji}");
                }

                sb.Append("\n\n");

                foreach (var captainGroup in captainGroups)
                {
                    sb.Append($"{captainGroup.Emoji} = {captainGroup.Captain.FirstName} {captainGroup.Captain.SecondName} ({captainGroup.Count})\n");
                }

                return sb.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return $"Oops: {e.Message}";
            }
        }

        private static string GetCaptainCountEmoji(int i)
        {
            return i switch
            {
                0 => ":first_place_medal:",
                1 => ":second_place_medal:",
                2 => ":third_place_medal:",
                _ => Constants.Emojis.NatureEmojis.GetRandom()
            };
        }

        private async Task<IEnumerable<EntryCaptainPick>> GetEntryCaptainPicks(int gameweek, int leagueId)
        {
            var leagueTask = _leagueClient.GetClassicLeague(leagueId);
            var playersTask = _playerClient.GetAllPlayers();

            var league = await leagueTask;
            var players = await playersTask;

            var entryCaptainPicks = await Task.WhenAll(league.Standings.Entries.OrderBy(x => x.Rank)
                .Select(entry => GetEntryCaptainPick(entry, gameweek, players)));

            return entryCaptainPicks.WhereNotNull();
        }

        private async Task<EntryCaptainPick> GetEntryCaptainPick(ClassicLeagueEntry entry, int gameweek, ICollection<Player> players)
        {
            try
            {
                var entryPicksTask = _entryClient.GetPicks(entry.Entry, gameweek);
                var hasUsedTripleCaptainForGameWeekTask = _chipsPlayed.GetHasUsedTripleCaptainForGameWeek(gameweek, entry.Entry);

                var entryPicks = await entryPicksTask;

                var captain = players.SingleOrDefault(player => player.Id == entryPicks.Picks.Single(pick => pick.IsCaptain).PlayerId);
                var viceCaptain = players.SingleOrDefault(player => player.Id == entryPicks.Picks.Single(pick => pick.IsViceCaptain).PlayerId);

                return new EntryCaptainPick
                {
                    Entry = entry,
                    Captain = captain,
                    ViceCaptain = viceCaptain,
                    IsTripleCaptain = await hasUsedTripleCaptainForGameWeekTask
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        private class EntryCaptainPick
        {
            public ClassicLeagueEntry Entry { get; set; }
            public Player Captain { get; set; }
            public Player ViceCaptain { get; set; }
            public bool IsTripleCaptain { get; set; }
        }
    }
}