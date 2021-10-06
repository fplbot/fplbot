using System.Collections.Generic;
using System.Threading.Tasks;
using FplBot.VerifiedEntries.Data.Abstractions;
using FplBot.VerifiedEntries.Data.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FplBot.VerifiedEntries.Data.Repositories
{
    internal class VerifiedPLEntriesRepository : IVerifiedPLEntriesRepository
    {
        private IConnectionMultiplexer _redis;
        private readonly ILogger<VerifiedPLEntriesRepository> _logger;
        private IDatabase _db;
        private string _server;

        public VerifiedPLEntriesRepository(IConnectionMultiplexer redis, IOptions<VerifiedRedisOptions> redisOptions, ILogger<VerifiedPLEntriesRepository> logger)
        {
            _redis = redis;
            _logger = logger;
            _db = _redis.GetDatabase();
            _server = redisOptions.Value.GetRedisServerHostAndPort;
        }

        public async Task Insert(VerifiedPLEntry entry)
        {
            await _db.HashSetAsync($"pl-entry-{entry.EntryId}", AllWriteFields(entry));
        }

        public async Task<IEnumerable<VerifiedPLEntry>> GetAllVerifiedPLEntries()
        {
            var allTeamKeys = _redis.GetServer(_server).Keys(pattern: "pl-entry-*", pageSize:10000);
            var redisValues = new List<Task<RedisValue[]>>();
            foreach (var key in allTeamKeys)
            {
                redisValues.Add(_db.HashGetAsync(key, AllQueryFields()));
            }

            var allEntries = await Task.WhenAll(redisValues);
            var verifiedEntries = new List<VerifiedPLEntry>();
            foreach (RedisValue[] fetchedTeamData in allEntries)
            {
                verifiedEntries.Add(ToVerifiedEntry(fetchedTeamData));
            }

            return verifiedEntries;
        }

        public async Task<VerifiedPLEntry> GetVerifiedPLEntry(int entryId)
        {
            var fetchedTeamData = await _db.HashGetAsync($"pl-entry-{entryId}", AllQueryFields());
            if (fetchedTeamData[0] == RedisValue.Null)
                return null;
            return ToVerifiedEntry(fetchedTeamData);
        }

        public Task Delete(int entryId)
        {
            return _db.KeyDeleteAsync($"pl-entry-{entryId}");
        }

        public async Task DeleteAll()
        {
            var allTeamKeys = _redis.GetServer(_server).Keys(pattern: "pl-entry-*", pageSize:10000);
            var tasks = new List<Task>();
            foreach (var key in allTeamKeys)
            {
                _logger.LogDebug($"Deleting key {key}");
                tasks.Add(_db.KeyDeleteAsync($"{key}"));
            }

            await Task.WhenAll(tasks);
        }

        public async Task DeleteAllOfThese(int[] entryIds)
        {
            var tasks = new List<Task>();
            foreach (var key in entryIds)
            {
                _logger.LogDebug($"Deleting key {key}");
                tasks.Add(_db.KeyDeleteAsync($"pl-entry-{key}"));
            }

            await Task.WhenAll(tasks);
        }

        public async Task UpdateStats(int entryId, SelfOwnershipStats selfOwnershipStats)
        {
            var entry = await GetVerifiedPLEntry(entryId);
            var newEntry = entry with { SelfOwnershipStats = selfOwnershipStats};
            await Insert(newEntry);// insert behaves as update
        }

        private static HashEntry[] AllWriteFields(VerifiedPLEntry entry)
        {
            var hashEntries = new List<HashEntry>()
            {
                new("entryId", entry.EntryId),
                new("teamId", entry.TeamId),
                new("teamCode", entry.TeamCode),
                new("teamName", entry.TeamName),
                new("playerId", entry.PlayerId),
                new("playerCode", entry.PlayerCode),
                new("playerWebName", entry.PlayerWebName),
                new("playerFullname", entry.PlayerFullName)
            };

            if (entry.SelfOwnershipStats != null)
            {
                hashEntries.Add(new("selfOwnerShipWeekCount", entry.SelfOwnershipStats.WeekCount));
                hashEntries.Add(new("selfOwnerShipTotalPoints", entry.SelfOwnershipStats.TotalPoints));
                hashEntries.Add(new("gameweek", entry.SelfOwnershipStats.Gameweek));
            };

            return hashEntries.ToArray();
        }

        private static RedisValue[] AllQueryFields()
        {
            return new RedisValue[]
            {
                "entryId",
                "teamId",
                "teamCode",
                "teamName",
                "playerId",
                "playerCode",
                "playerWebName",
                "playerFullname",
                "selfOwnerShipWeekCount",
                "selfOwnerShipTotalPoints",
                "gameweek"
            };
        }

        private static VerifiedPLEntry ToVerifiedEntry(RedisValue[] fetchedTeamData)
        {
            return new(
                EntryId: (int)fetchedTeamData[0],
                TeamId: (long) fetchedTeamData[1],
                TeamCode:(long)fetchedTeamData[2],
                TeamName: fetchedTeamData[3],
                PlayerId: (int) fetchedTeamData[4],
                PlayerCode: (int)fetchedTeamData[5],
                PlayerWebName: fetchedTeamData[6],
                PlayerFullName : fetchedTeamData[7],
                SelfOwnershipStats: fetchedTeamData.Length > 8 ?
                    new SelfOwnershipStats(
                        WeekCount: (int)fetchedTeamData[8],
                        TotalPoints: (int)fetchedTeamData[9],
                        Gameweek: (int) fetchedTeamData[10])
                    : null
            );
        }
    }

}
