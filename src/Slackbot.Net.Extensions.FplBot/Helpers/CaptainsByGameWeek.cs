using Fpl.Client.Abstractions;
using Fpl.Client.Models;
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
        private readonly IPlayerClient _playerClient;
        private readonly ILeagueClient _leagueClient;
        private readonly IEntryForGameweek _entryForGameweek;

        public CaptainsByGameWeek(IPlayerClient playerClient, ILeagueClient leagueClient, IEntryForGameweek entryForGameweek)
        {
            _playerClient = playerClient;
            _leagueClient = leagueClient;
            _entryForGameweek = entryForGameweek;
        }
        
        public async Task<string> GetCaptainsByGameWeek(int gameweek, int leagueId)
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

        public async Task<string> GetCaptainsChartByGameWeek(int gameweek, int leagueId)
        {
            
                var entryCaptainPicks = await GetEntryCaptainPicks(gameweek, leagueId);
                var captainGroups = entryCaptainPicks
                    .GroupBy(x => x.Captain.Id, el => el.Captain)
                    .OrderByDescending(x => x.Count())
                    .Select((group, i) => new { Captain = group.First(), Count = group.Count(), Emoji = Formatter.RankEmoji(i) })
                    .MaterializeToArray();

                var sb = new StringBuilder();
                sb.Append($":bar_chart: *Captain picks chart for gameweek {gameweek}*\n\n");

                var max = captainGroups.Max(x => x?.Count);

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

        private async Task<IEnumerable<EntryCaptainPick>> GetEntryCaptainPicks(int gameweek, int leagueId)
        {
            var leagueTask = _leagueClient.GetClassicLeague(leagueId);
            var playersTask = _playerClient.GetAllPlayers();

            var league = await leagueTask;
            var players = await playersTask;

            var entries = new List<GenericEntry>();
            if (league.Standings.Entries.Any())
            {
                entries = league.Standings.Entries.OrderBy(x => x.Rank).ToList().Select(e => new GenericEntry
                {
                    Entry = e.Entry,
                    EntryName = e.EntryName
                
                }).ToList();
            }
            else
            {
                entries = league.NewEntries.Entries.ToList().Select(e => new GenericEntry
                {
                    Entry = e.Entry,
                    EntryName = e.EntryName
                
                }).ToList();
            }
            
            var entryCaptainPicks = await Task.WhenAll(entries.Select(entry => GetEntryCaptainPick(entry, gameweek, players)));

            return entryCaptainPicks.WhereNotNull();
        }

        private async Task<EntryCaptainPick> GetEntryCaptainPick(GenericEntry entry, int gameweek, ICollection<Player> players)
        {
            try
            {
                var entryForGameweekTask = _entryForGameweek.GetEntryForGameweek(entry, gameweek);
                var entryPicksForGameweek = await entryForGameweekTask;

                return new EntryCaptainPick
                {
                    Entry = entry,
                    Captain = players.SingleOrDefault(player => player.Id == entryPicksForGameweek.Captain.PlayerId),
                    ViceCaptain = players.SingleOrDefault(player => player.Id == entryPicksForGameweek.ViceCaptain.PlayerId),
                    IsTripleCaptain = entryPicksForGameweek.ActiveChip == Constants.ChipNames.TripleCaptain
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
            public GenericEntry Entry { get; set; }
            public Player Captain { get; set; }
            public Player ViceCaptain { get; set; }
            public bool IsTripleCaptain { get; set; }
        }
    }

    public class GenericEntry
    {
        public int Entry { get; set; }
        public string EntryName { get; set; }
    }
}