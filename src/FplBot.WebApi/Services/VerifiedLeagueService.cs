using System.Collections.Generic;
using Fpl.Client.Abstractions;
using Fpl.Search;
using Fpl.Search.Models;
using FplBot.Core.Extensions;
using FplBot.WebApi.Models;
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

        public VerifiedLeagueService(
            IEntryClient entryClient, 
            IEntryHistoryClient entryHistoryClient, 
            IGameweekClient gameweekClient,
            IPlayerClient playerClient,
            ITeamsClient teamsClient)
        {
            _entryClient = entryClient;
            _entryHistoryClient = entryHistoryClient;
            _gameweekClient = gameweekClient;
            _playerClient = playerClient;
            _teamsClient = teamsClient;
        }

        public async Task<VerifiedLeagueModel> GetStandings()
        {
            var allVerifiedEntriesInPL = VerifiedEntries.VerifiedEntriesMap.Keys.Where(k =>
                VerifiedEntries.VerifiedEntriesMap[k] == VerifiedEntryType.FootballerInPL).ToArray();

            var gameweekTask = _gameweekClient.GetGameweeks();
            var allPlayersTask = _playerClient.GetAllPlayers();
            var allTeamsTask = _teamsClient.GetAllTeams();

            var currentGameweek = (await gameweekTask).GetCurrentGameweek().Id;

            var entryTaskDict = allVerifiedEntriesInPL.ToDictionary(x => x, x => new
            {
                HistoryTask = _entryHistoryClient.GetHistory(x),
                InfoTask = _entryClient.Get(x),
                PicksTask = _entryClient.GetPicks(x, currentGameweek)
            });

            var allPlayers = await allPlayersTask;
            var allTeams = await allTeamsTask;

            var entries = new List<VerifiedLeagueItem>();
            foreach (int entryId in allVerifiedEntriesInPL)
            {
                var history = await entryTaskDict[entryId].HistoryTask;
                var info = await entryTaskDict[entryId].InfoTask;
                var picks = await entryTaskDict[entryId].PicksTask;

                var captainId = picks.Picks.SingleOrDefault(p => p.IsCaptain)?.PlayerId;
                var viceCaptainId = picks.Picks.SingleOrDefault(p => p.IsViceCaptain)?.PlayerId;
                var currentGw = history.GameweekHistory.LastOrDefault();
                var lastGw = history.GameweekHistory.Count > 1 ? history.GameweekHistory.ElementAtOrDefault(history.GameweekHistory.Count - 2) : currentGw;
                var plPlayer = VerifiedEntries.VerifiedPLEntryToPlayerIdMap.ContainsKey(entryId)
                    ? allPlayers.Get(VerifiedEntries.VerifiedPLEntryToPlayerIdMap[entryId])
                    : null;

                entries.Add(new VerifiedLeagueItem
                {
                    EntryId = entryId,
                    RealName = info.PlayerFullName,
                    TeamName = info.TeamName,
                    PlName = plPlayer?.FullName,
                    PlaysForTeam = allTeams.Get(plPlayer?.TeamId)?.Name,
                    ShirtImageUrl = $"https://fantasy.premierleague.com/dist/img/shirts/standard/shirt_{plPlayer?.TeamCode}-66.png",
                    TotalPoints = currentGw?.TotalPoints,
                    TotalPointsLastGw = lastGw?.TotalPoints,
                    OverallRank = currentGw?.OverallRank,
                    PointsThisGw = currentGw?.Points,
                    ChipUsed = picks.ActiveChip,
                    Captain = captainId.HasValue ? allPlayers.Get(captainId)?.WebName : null,
                    ViceCaptain = viceCaptainId.HasValue ? allPlayers.Get(viceCaptainId)?.WebName : null
                });
            }

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
    }

    public interface IVerifiedLeagueService
    {
        Task<VerifiedLeagueModel> GetStandings();
    }
}