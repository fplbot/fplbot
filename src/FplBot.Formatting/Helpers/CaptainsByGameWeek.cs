using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fpl.Client;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;

namespace FplBot.Formatting.Helpers
{
    public class CaptainsByGameWeek : ICaptainsByGameWeek
    {
        private readonly IGlobalSettingsClient _globalSettingsClient;
        private readonly ILeagueClient _leagueClient;
        private readonly IEntryForGameweek _entryForGameweek;
        private readonly ILogger<CaptainsByGameWeek> _logger;

        public CaptainsByGameWeek(IGlobalSettingsClient globalSettingsClient, ILeagueClient leagueClient, IEntryForGameweek entryForGameweek, ILogger<CaptainsByGameWeek> logger)
        {
            _globalSettingsClient = globalSettingsClient;
            _leagueClient = leagueClient;
            _entryForGameweek = entryForGameweek;
            _logger = logger;
        }

        public async Task<string> GetCaptainsByGameWeek(int gameweek, int leagueId, bool includeExternalLinks = true)
        {

                var entryCaptainPicks = await GetEntryCaptainPicks(gameweek, leagueId);

                var sb = new StringBuilder();
                sb.Append($"💥 *Captain picks for gameweek {gameweek}*\n");

                foreach (var entryCaptainPick in entryCaptainPicks)
                {
                    var captain = entryCaptainPick.Captain;
                    var viceCaptain = entryCaptainPick.ViceCaptain;

                    string entryLinkOrName = includeExternalLinks ?  entryCaptainPick.Entry.GetEntryLink(gameweek) : entryCaptainPick.Entry.EntryName;
                    sb.Append($"*{entryLinkOrName}* - {captain.FirstName} {captain.SecondName} ({viceCaptain.FirstName} {viceCaptain.SecondName}) ");
                    if (entryCaptainPick.IsTripleCaptain)
                    {
                        sb.Append("TRIPLECAPPED!! 🚀🚀🚀");
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
                sb.Append($"📊 *Captain picks chart for gameweek {gameweek}*\n\n");

                var max = captainGroups.Max(x => x?.Count);

                for (var i = max; i > 0; i--)
                {
                    foreach (var captainGroup in captainGroups)
                    {
                        if (captainGroup.Count >= i)
                        {
                            sb.Append("⬛");
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
            var playersTask = _globalSettingsClient.GetGlobalSettings();

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

            var entryCaptainPicks = await Task.WhenAll(entries.Select(entry => GetEntryCaptainPick(entry, gameweek, players.Players)));

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
                    IsTripleCaptain = entryPicksForGameweek.ActiveChip == FplConstants.ChipNames.TripleCaptain
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return null;
            }
        }
    }
}
