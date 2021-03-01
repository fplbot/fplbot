using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FplBot.Core.Data
{
    internal class VerifiedEntriesRepository : IVerifiedEntriesRepository
    {
        private ConnectionMultiplexer _redis;
        private readonly ILogger<RedisSlackTeamRepository> _logger;
        private IDatabase _db;
        private string _server;

        public VerifiedEntriesRepository(ConnectionMultiplexer redis, IOptions<RedisOptions> redisOptions, ILogger<RedisSlackTeamRepository> logger)
        {
            _redis = redis;
            _logger = logger;
            _db = _redis.GetDatabase();
            _server = redisOptions.Value.GetRedisServerHostAndPort;
        }
        
        public async Task Insert(VerifiedEntry entry)
        {
            await _db.HashSetAsync($"entry-{entry.EntryId}", AllWriteFields(entry));
        }

        public async Task<IEnumerable<VerifiedEntry>> GetAllVerifiedEntries()
        {
            var allTeamKeys = _redis.GetServer(_server).Keys(pattern: "entry-*", pageSize:10000);
            var redisValues = new List<Task<RedisValue[]>>();
            foreach (var key in allTeamKeys)
            {
                redisValues.Add(_db.HashGetAsync(key, AllQueryFields()));
            }

            var allEntries = await Task.WhenAll(redisValues);
            var verifiedEntries = new List<VerifiedEntry>();
            foreach (RedisValue[] fetchedTeamData in allEntries)
            {
                verifiedEntries.Add(ToVerifiedEntry(fetchedTeamData));
            }

            return verifiedEntries;
        }

        public async Task<VerifiedEntry> GetVerifiedEntry(int entryId)
        {
            var fetchedTeamData = await _db.HashGetAsync($"entry-{entryId}", AllQueryFields());
            if (fetchedTeamData[0] == RedisValue.Null)
                return null;
            return ToVerifiedEntry(fetchedTeamData);
        }

        public Task Delete(int entryId)
        {
            return _db.KeyDeleteAsync($"entry-{entryId}");
        }

        public async Task DeleteAll()
        {
            var allTeamKeys = _redis.GetServer(_server).Keys(pattern: "entry-*", pageSize:10000);
            var tasks = new List<Task>();
            foreach (var key in allTeamKeys)
            {
                _logger.LogDebug($"Deleting key {key}");
                tasks.Add(_db.KeyDeleteAsync($"{key}"));
            }

            await Task.WhenAll(tasks);
        }

        public async Task UpdateAllStats(int entryId, VerifiedEntryStats verifiedEntryStats)
        {
            var entry = await GetVerifiedEntry(entryId);
            var newEntry = entry with {EntryStats = verifiedEntryStats};
            await Insert(newEntry);// insert behaves as update
        }

        public async Task UpdateLiveStats(int entryId, VerifiedEntryPointsUpdate newStats)
        {
            var entry = await GetVerifiedEntry(entryId);
            var verifiedEntryStats = entry.EntryStats with
            {
                CurrentGwTotalPoints = newStats.CurrentGwTotalPoints,
                OverallRank = newStats.OverallRank,
                PointsThisGw = newStats.PointsThisGw
            };
            
            var newEntry = entry with
            {
                EntryStats = verifiedEntryStats
            };

            await Insert(newEntry); // insert behaves as update
        }

        private static HashEntry[] AllWriteFields(VerifiedEntry entry)
        {
            var hashEntries = new List<HashEntry>
            {
                new("entryId", entry.EntryId), new("fullName", entry.FullName),
                new("entryTeamName", entry.EntryTeamName),
                new("verifiedEntryType", entry.VerifiedEntryType.ToString())
            };

            if (entry.EntryStats != null)
            {
                hashEntries.Add(new("currentGwTotalPoints", entry.EntryStats.CurrentGwTotalPoints));
                hashEntries.Add(new("lastGwTotalPoints", entry.EntryStats.LastGwTotalPoints));
                hashEntries.Add(new("overAllRank", entry.EntryStats.OverallRank));
                hashEntries.Add(new("pointsThisGw", entry.EntryStats.PointsThisGw));
                hashEntries.Add(new("activeChip", !string.IsNullOrEmpty(entry.EntryStats?.ActiveChip) ? entry.EntryStats?.ActiveChip : string.Empty));
                hashEntries.Add(new("captain", entry.EntryStats.Captain));
                hashEntries.Add(new("viceCaptain", entry.EntryStats.ViceCaptain));
                hashEntries.Add(new("gameweek", entry.EntryStats.Gameweek));
            }

            return hashEntries.ToArray();
        }

        private static RedisValue[] AllQueryFields()
        {
            return new RedisValue[]
            {
                "entryId", 
                "fullName",
                "entryTeamName",
                "verifiedEntryType",
                "currentGwTotalPoints",
                "lastGwTotalPoints",
                "overAllRank",
                "pointsThisGw",
                "activeChip",
                "captain",
                "viceCaptain",
                "gameweek",
            };
        }
        
        private static VerifiedEntry ToVerifiedEntry(RedisValue[] fetchedTeamData)
        {
            Enum.TryParse(fetchedTeamData[3], out VerifiedEntryType verifiedEntryType);
            return new(
                EntryId: (int)fetchedTeamData[0], 
                FullName: fetchedTeamData[1], 
                EntryTeamName: fetchedTeamData[2], 
                VerifiedEntryType:verifiedEntryType, 
                EntryStats: fetchedTeamData.Length > 3 ? new VerifiedEntryStats(
                    CurrentGwTotalPoints: (int)fetchedTeamData[4],
                    LastGwTotalPoints: (int)fetchedTeamData[5],
                    OverallRank : (int)fetchedTeamData[6],
                    PointsThisGw : (int)fetchedTeamData[7],
                    ActiveChip : fetchedTeamData[8],
                    Captain : fetchedTeamData[9],
                    ViceCaptain : fetchedTeamData[10],
                    Gameweek: (int) fetchedTeamData[11]
                ) : null
                );
        }
    }

    public record VerifiedEntry(
        int EntryId,
        string FullName,
        string EntryTeamName,
        VerifiedEntryType VerifiedEntryType,
        VerifiedEntryStats EntryStats = null
);

    public record VerifiedEntryStats(
        int CurrentGwTotalPoints,
        int LastGwTotalPoints,
        int OverallRank,
        int PointsThisGw,
        string ActiveChip,
        string Captain,
        string ViceCaptain,
        int Gameweek);

    public record VerifiedEntryPointsUpdate(
        int CurrentGwTotalPoints, 
        int OverallRank, 
        int PointsThisGw);

    public interface IVerifiedEntriesRepository
    {
        Task Insert(VerifiedEntry entry);
        Task<IEnumerable<VerifiedEntry>> GetAllVerifiedEntries();
        Task<VerifiedEntry> GetVerifiedEntry(int entryId);
        
        Task Delete(int entryId);
        Task DeleteAll();
        Task UpdateAllStats(int entryId, VerifiedEntryStats verifiedEntryStats);
        Task UpdateLiveStats(int entryId, VerifiedEntryPointsUpdate newStats);
    }
}