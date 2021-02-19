using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.Search;
using Fpl.Search.Models;
using FplBot.Core.Extensions;
using FplBot.WebApi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FplBot.WebApi.Services
{


    public class VerifiedLeagueService : IVerifiedLeagueService
    {
        private readonly IEntryClient _entryClient;
        private readonly IEntryHistoryClient _entryHistoryClient;
        private readonly IGameweekClient _gameweekClient;
        private readonly IPlayerClient _playerClient;
        private readonly ITeamsClient _teamsClient;
        private readonly ILiveClient _liveClient;

        public VerifiedLeagueService(
            IEntryClient entryClient, 
            IEntryHistoryClient entryHistoryClient, 
            IGameweekClient gameweekClient,
            IPlayerClient playerClient,
            ITeamsClient teamsClient,
            ILiveClient liveClient)
        {
            _entryClient = entryClient;
            _entryHistoryClient = entryHistoryClient;
            _gameweekClient = gameweekClient;
            _playerClient = playerClient;
            _teamsClient = teamsClient;
            _liveClient = liveClient;
        }

        public async Task<VerifiedLeagueModel> GetStandings()
        {
            var allVerifiedEntriesInPL = VerifiedEntries.VerifiedEntriesMap.Keys.Where(k =>
                VerifiedEntries.VerifiedEntriesMap[k] == VerifiedEntryType.FootballerInPL).ToArray();

            var (currentGameweek, allPlayers, allTeams) = await GetBootstrap();

            var entries = await Task.WhenAll(allVerifiedEntriesInPL.Select(entryId => GetVerifiedLeagueItem(entryId, currentGameweek, allPlayers, allTeams)));

            var entriesOrderedByRank = entries.OrderByDescending(e => e.TotalPoints).ToArray();
            var lastGwOrder = entries.OrderByDescending(e => e.TotalPointsLastGw).ToList();

            var currentRank = 0;
            foreach (var item in entriesOrderedByRank)
            {
                var lastGwRank = lastGwOrder.FindIndex(x => x.EntryId == item.EntryId);
                item.Movement = currentRank - lastGwRank;
                currentRank++;
            }

            return new VerifiedLeagueModel { Gameweek = currentGameweek, Entries = entriesOrderedByRank };
        }

        public async Task<VerifiedPLEntryModel> GetVerifiedTeam(string slug)
        {
            var entry = VerifiedEntries.VerifiedPLEntries.SingleOrDefault(v => v.Slug == slug);

            if (entry == null)
            {
                return null;
            }

            var (currentGameweek, allPlayers, allTeams) = await GetBootstrap();
            var verifiedLeagueItem = await GetVerifiedLeagueItem(entry.EntryId, currentGameweek, allPlayers, allTeams);

            var selfOwnerShip = (await GetSelfOwnershipPoints(verifiedLeagueItem.EntryId, verifiedLeagueItem.PLPlayerId, currentGameweek)).ToArray();

            return new VerifiedPLEntryModel
            {
                EntryId = verifiedLeagueItem.EntryId,
                TeamName = verifiedLeagueItem.TeamName,
                RealName = verifiedLeagueItem.RealName,
                PlName = verifiedLeagueItem.PLName,
                PlaysForTeam = verifiedLeagueItem.PlaysForTeam,
                ShirtImageUrl = verifiedLeagueItem.ShirtImageUrl,
                ImageUrl = verifiedLeagueItem.ImageUrl,
                PointsThisGw = verifiedLeagueItem.PointsThisGw,
                TotalPoints = verifiedLeagueItem.TotalPoints,
                OverallRank = verifiedLeagueItem.OverallRank,
                Captain = verifiedLeagueItem.Captain,
                ViceCaptain = verifiedLeagueItem.ViceCaptain,
                ChipUsed = verifiedLeagueItem.ChipUsed,
                SelfOwnershipWeekCount = selfOwnerShip.Length,
                SelfOwnershipTotalPoints = selfOwnerShip.Sum()
            };
        }

        private async Task<IEnumerable<int>> GetSelfOwnershipPoints(int entryId, int? plPlayerId, int gameweek)
        {
            var allPicks = await Task.WhenAll(Enumerable.Range(1, gameweek).Select(gw => GetPick(entryId, gw)));

            var selfPicks = allPicks
                .Select(p => (p.Gameweek, SelfPick: p.Pick.Picks.SingleOrDefault(pick => pick.PlayerId == plPlayerId)))
                .Where(x => x.SelfPick != null)
                .ToArray();

            if (!selfPicks.Any())
            {
                return Enumerable.Empty<int>();
            }

            var gwPointsForSelfPick = await Task.WhenAll(selfPicks.Select(s => GetPickScore(s.Gameweek, s.SelfPick.PlayerId, s.SelfPick.Multiplier)));
            
            return gwPointsForSelfPick;
        }

        private async Task<GameweekPick> GetPick(int entryId, int gw)
        {
            var picks = await _entryClient.GetPicks(entryId, gw);
            return new GameweekPick(gw, picks);
        }

        private async Task<int> GetPickScore(int gw, int playerId, int multiplier)
        {
            var liveItemsForGw = await _liveClient.GetLiveItems(gw);
            return (liveItemsForGw.SingleOrDefault(x => x.Id == playerId)?.Stats?.TotalPoints ?? 0) * multiplier;
        }

        private async Task<(int, ICollection<Player>, ICollection<Team>)> GetBootstrap()
        {
            var gameweekTask = _gameweekClient.GetGameweeks();
            var allPlayersTask = _playerClient.GetAllPlayers();
            var allTeamsTask = _teamsClient.GetAllTeams();

            var currentGameweek = (await gameweekTask).GetCurrentGameweek().Id;
            var allPlayers = await allPlayersTask;
            var allTeams = await allTeamsTask;

            return (currentGameweek, allPlayers, allTeams);
        }

        private async Task<VerifiedLeagueItem> GetVerifiedLeagueItem(int entryId, int gameweek, ICollection<Player> allPlayers, ICollection<Team> allTeams)
        {
            var historyTask = _entryHistoryClient.GetHistory(entryId);
            var infoTask = _entryClient.Get(entryId);
            var picksTask = _entryClient.GetPicks(entryId, gameweek);

            var history = await historyTask;
            var info = await infoTask;
            var picks = await picksTask;

            var captainId = picks.Picks.SingleOrDefault(p => p.IsCaptain)?.PlayerId;
            var viceCaptainId = picks.Picks.SingleOrDefault(p => p.IsViceCaptain)?.PlayerId;
            var currentGw = history.GameweekHistory.LastOrDefault();
            var lastGw = history.GameweekHistory.Count > 1
                ? history.GameweekHistory.ElementAtOrDefault(history.GameweekHistory.Count - 2)
                : currentGw;
            var verifiedPlEntry = VerifiedEntries.VerifiedPLEntries.SingleOrDefault(x => x.EntryId == entryId);
            var plPlayer = allPlayers.Get(verifiedPlEntry?.PlayerId);

            return new VerifiedLeagueItem
            {
                EntryId = entryId,
                Slug = verifiedPlEntry?.Slug,
                RealName = info.PlayerFullName,
                TeamName = info.TeamName,
                PLPlayerId = plPlayer?.Id,
                PLName = plPlayer?.FullName,
                PlaysForTeam = allTeams.Get(plPlayer?.TeamId)?.Name,
                ShirtImageUrl = $"https://fantasy.premierleague.com/dist/img/shirts/standard/shirt_{plPlayer?.TeamCode}{(plPlayer.Position == FplPlayerPosition.Goalkeeper ? "_1" : null)}-66.png",
                ImageUrl = $"https://resources.premierleague.com/premierleague/photos/players/110x140/p{plPlayer.Code}.png",
                TotalPoints = currentGw?.TotalPoints,
                TotalPointsLastGw = lastGw?.TotalPoints,
                OverallRank = currentGw?.OverallRank,
                PointsThisGw = currentGw?.Points,
                ChipUsed = picks.ActiveChip,
                Captain = captainId.HasValue ? allPlayers.Get(captainId)?.WebName : null,
                ViceCaptain = viceCaptainId.HasValue ? allPlayers.Get(viceCaptainId)?.WebName : null
            };
        }
    }

    public interface IVerifiedLeagueService
    {
        Task<VerifiedLeagueModel> GetStandings();
        Task<VerifiedPLEntryModel> GetVerifiedTeam(string slug);
    }

    record GameweekPick(int Gameweek, EntryPicks Pick);
}