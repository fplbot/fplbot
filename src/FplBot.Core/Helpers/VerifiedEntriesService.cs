using System;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.Search;
using Fpl.Search.Models;
using FplBot.Core.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace FplBot.Core.Helpers
{
    public class VerifiedEntriesService : IVerifiedEntriesService
    {
        private readonly IEntryClient _entryClient;
        private readonly IEntryHistoryClient _entryHistoryClient;
        private readonly IGlobalSettingsClient _settingsClient;
        private readonly ILiveClient _liveClient;
        private readonly IDistributedCache _cache;

        private const string EntryCacheKeyPrefix = "VERIFIEDENTRY";
        private const string VerifiedentriesCacheKey = "VERIFIEDENTRIES";

        public VerifiedEntriesService(
            IEntryClient entryClient, 
            IEntryHistoryClient entryHistoryClient, 
            IGlobalSettingsClient settingsClient,
            ILiveClient liveClient,
            IDistributedCache cache)
        {
            _entryClient = entryClient;
            _entryHistoryClient = entryHistoryClient;
            _settingsClient = settingsClient;
            _liveClient = liveClient;
            _cache = cache;
        }

        public async Task<IEnumerable<VerifiedPLEntryModel>> GetAllVerifiedPLEntries()
        {
            
            var cacheJson = await _cache.GetStringAsync(VerifiedentriesCacheKey);
            if (!string.IsNullOrEmpty(cacheJson))
            {
                return JsonConvert.DeserializeObject<VerifiedPLEntryModel[]>(cacheJson);
            }
            
            var allVerifiedEntriesInPL = VerifiedEntries.VerifiedEntriesMap.Keys.Where(k =>
                VerifiedEntries.VerifiedEntriesMap[k] == VerifiedEntryType.FootballerInPL).ToArray();

            var (currentGameweek, allPlayers, allTeams) = await GetBootstrap();

            var liveItems = await GetAllLiveItems(currentGameweek);
            var entries = await Task.WhenAll(allVerifiedEntriesInPL.Select(entryId => GetVerifiedPLEntry(entryId, currentGameweek, allPlayers, allTeams, liveItems)));

            var entriesOrderedByRank = entries.OrderByDescending(e => e.TotalPoints).ToList();
            var lastGwOrder = entries.OrderByDescending(e => e.TotalPointsLastGw).ToList();

            var currentRank = 0;
            foreach (var item in entriesOrderedByRank)
            {
                var lastGwRank = lastGwOrder.FindIndex(x => x.EntryId == item.EntryId);
                item.Movement = currentRank - lastGwRank;
                currentRank++;
            }

            await _cache.SetStringAsync(VerifiedentriesCacheKey, JsonConvert.SerializeObject(entriesOrderedByRank), new DistributedCacheEntryOptions{ AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)});
            return entriesOrderedByRank;
        }

        private async Task<ICollection<LiveItem>[]> GetAllLiveItems(int currentGameweek)
        {
            var liveItems = await Task.WhenAll(GetGameweekNumbersUpUntilCurrent(currentGameweek)
                .Select(gw => _liveClient.GetLiveItems(gw, gw == currentGameweek)));
            return liveItems;
        }

        public async Task<VerifiedPLEntryModel> GetVerifiedPLEntry(string slug)
        {
            var cacheJson = await _cache.GetStringAsync($"{EntryCacheKeyPrefix}-{slug}");
            if (!string.IsNullOrEmpty(cacheJson))
            {
                return JsonConvert.DeserializeObject<VerifiedPLEntryModel>(cacheJson);
            }

            var entry = VerifiedEntries.VerifiedPLEntries.SingleOrDefault(v => v.Slug == slug);

            if (entry == null)
            {
                return null;
            }

            var (currentGameweek, allPlayers, allTeams) = await GetBootstrap();
            var liveItems = await GetAllLiveItems(currentGameweek);
            var verifiedPLEntry = await GetVerifiedPLEntry(entry.EntryId, currentGameweek, allPlayers, allTeams, liveItems);
            await _cache.SetStringAsync($"{EntryCacheKeyPrefix}-{slug}", JsonConvert.SerializeObject(verifiedPLEntry), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)});

            return verifiedPLEntry;
        }

        private async Task<IEnumerable<int>> GetSelfOwnershipPoints(int entryId, int? plPlayerId, int gameweek, ICollection<LiveItem>[] liveItems)
        {
            var allPicks = await Task.WhenAll(GetGameweekNumbersUpUntilCurrent(gameweek).Select(gw => GetPick(entryId, gw)));

            var selfPicks = allPicks
                .Where(p => p.Pick != null)
                .Select(p => (p.Gameweek, SelfPick: p.Pick.Picks.SingleOrDefault(pick => pick.PlayerId == plPlayerId)))
                .Where(x => x.SelfPick != null)
                .ToArray();

            if (!selfPicks.Any())
            {
                return Enumerable.Empty<int>();
            }

            var gwPointsForSelfPick = selfPicks.Select(s => GetPickScore(liveItems[s.Gameweek - 1], s.SelfPick.PlayerId, s.SelfPick.Multiplier));
            
            return gwPointsForSelfPick;
        }

        private static IEnumerable<int> GetGameweekNumbersUpUntilCurrent(int gameweek)
        {
            return Enumerable.Range(1, gameweek);
        }

        private async Task<GameweekPick> GetPick(int entryId, int gw)
        {
            var picks = await _entryClient.GetPicks(entryId, gw, tolerate404: true);
            return new GameweekPick(gw, picks);
        }

        public int GetPickScore(ICollection<LiveItem> liveItems, int playerId, int multiplier)
        {
            return (liveItems.SingleOrDefault(x => x.Id == playerId)?.Stats?.TotalPoints ?? 0) * multiplier;
        }

        private async Task<(int, ICollection<Player>, ICollection<Team>)> GetBootstrap()
        {
            var globalSettings = await _settingsClient.GetGlobalSettings();
            var currentGameweek = globalSettings.Gameweeks.GetCurrentGameweek().Id;
            return (currentGameweek, globalSettings.Players, globalSettings.Teams);
        }

        private async Task<VerifiedPLEntryModel> GetVerifiedPLEntry(int entryId, int gameweek, ICollection<Player> allPlayers, ICollection<Team> allTeams, ICollection<LiveItem>[] liveItems)
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
            var selfOwnerShip = (await GetSelfOwnershipPoints(entryId, plPlayer?.Id, gameweek, liveItems)).ToArray();

            return new VerifiedPLEntryModel
            {
                EntryId = entryId,
                Slug = verifiedPlEntry?.Slug,
                RealName = info.PlayerFullName,
                TeamName = info.TeamName,
                PLPlayerId = plPlayer?.Id,
                PLName = plPlayer?.FullName,
                PlaysForTeam = allTeams.Get(plPlayer?.TeamId)?.Name,
                ShirtImageUrl = $"https://fantasy.premierleague.com/dist/img/shirts/standard/shirt_{plPlayer?.TeamCode}" +
                                $"{(plPlayer?.Position == FplPlayerPosition.Goalkeeper ? "_1" : null)}-66.png",
                ImageUrl = $"https://resources.premierleague.com/premierleague/photos/players/110x140/p{plPlayer.Code}.png",
                TotalPoints = currentGw?.TotalPoints,
                TotalPointsLastGw = lastGw?.TotalPoints,
                OverallRank = currentGw?.OverallRank,
                PointsThisGw = currentGw?.Points,
                ChipUsed = picks.ActiveChip,
                Captain = captainId.HasValue ? allPlayers.Get(captainId)?.WebName : null,
                ViceCaptain = viceCaptainId.HasValue ? allPlayers.Get(viceCaptainId)?.WebName : null,
                SelfOwnershipWeekCount = selfOwnerShip.Length,
                SelfOwnershipTotalPoints = selfOwnerShip.Sum(),
                Gameweek = gameweek
            };
        }
    }

    public interface IVerifiedEntriesService
    {
        Task<IEnumerable<VerifiedPLEntryModel>> GetAllVerifiedPLEntries();
        Task<VerifiedPLEntryModel> GetVerifiedPLEntry(string slug);
    }

    class GameweekPick
    {
        public int Gameweek { get; }
        public EntryPicks Pick { get; }

        public GameweekPick(int gameweek, EntryPicks pick)
        {
            Gameweek = gameweek;
            Pick = pick;
        }
    }

    public class VerifiedPLEntryModel
    {
        public int EntryId { get; set; }
        public string Slug { get; set; }
        public string TeamName { get; set; }
        public string RealName { get; set; }
        public int? PLPlayerId { get; set; }
        public string PLName { get; set; }
        public string PlaysForTeam { get; set; }
        public string ShirtImageUrl { get; set; }
        public string ImageUrl { get; set; }
        public int? PointsThisGw { get; set; }
        public int? TotalPointsLastGw { get; set; }
        public int? TotalPoints { get; set; }
        public int? OverallRank { get; set; }
        public int Movement { get; set; }
        public string Captain { get; set; }
        public string ViceCaptain { get; set; }
        public string ChipUsed { get; set; }
        public int SelfOwnershipWeekCount { get; set; }
        public int SelfOwnershipTotalPoints { get; set; }
        public int Gameweek { get; set; }
    }
}