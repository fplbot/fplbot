using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FplBot.VerifiedEntries.Data.Abstractions;
using FplBot.VerifiedEntries.Data.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FplBot.VerifiedEntries.Data.Repositories
{
    internal class VerifiedEntriesRepository : IVerifiedEntriesRepository
    {
        private IConnectionMultiplexer _redis;
        private readonly ILogger<VerifiedEntriesRepository> _logger;
        private IDatabase _db;
        private string _server;

        public VerifiedEntriesRepository(IConnectionMultiplexer redis, IOptions<VerifiedRedisOptions> redisOptions, ILogger<VerifiedEntriesRepository> logger)
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
                new("entryId", entry.EntryId),
                new("fullName", entry.FullName),
                new("entryTeamName", entry.EntryTeamName),
                new("verifiedEntryType", entry.VerifiedEntryType.ToString()),
                new("alias", entry.Alias ?? string.Empty),
                new("description", entry.Description ?? string.Empty),
            };

            if (entry.EntryStats != null)
            {
                hashEntries.Add(new("currentGwTotalPoints", entry.EntryStats.CurrentGwTotalPoints));
                hashEntries.Add(new("lastGwTotalPoints", entry.EntryStats.LastGwTotalPoints));
                hashEntries.Add(new("overAllRank", entry.EntryStats.OverallRank));
                hashEntries.Add(new("pointsThisGw", entry.EntryStats.PointsThisGw));
                hashEntries.Add(new("activeChip", ValueOrStringEmpty(entry.EntryStats.ActiveChip)));
                hashEntries.Add(new("captain", ValueOrStringEmpty(entry.EntryStats.Captain)));
                hashEntries.Add(new("viceCaptain", ValueOrStringEmpty(entry.EntryStats.ViceCaptain)));
                hashEntries.Add(new("gameweek", entry.EntryStats.Gameweek));

                string ValueOrStringEmpty(string value)
                {
                    return !string.IsNullOrEmpty(value) ? value : string.Empty;
                }
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
                "alias",
                "description",
                "currentGwTotalPoints",
                "lastGwTotalPoints",
                "overAllRank",
                "pointsThisGw",
                "activeChip",
                "captain",
                "viceCaptain",
                "gameweek"
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
                Alias: fetchedTeamData[4],
                Description: fetchedTeamData[5],
                EntryStats: fetchedTeamData.Length > 5 ? new VerifiedEntryStats(
                    CurrentGwTotalPoints: (int)fetchedTeamData[6],
                    LastGwTotalPoints: (int)fetchedTeamData[7],
                    OverallRank : (int)fetchedTeamData[8],
                    PointsThisGw : (int)fetchedTeamData[9],
                    ActiveChip : fetchedTeamData[10],
                    Captain : fetchedTeamData[11],
                    ViceCaptain : fetchedTeamData[12],
                    Gameweek: (int) fetchedTeamData[13]
                ) : null
                );
        }
    }
}
