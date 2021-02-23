using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FplBot.Core.Data
{
    internal class VerifiedPLEntriesRepository : IVerifiedPLEntriesRepository
    {
        private ConnectionMultiplexer _redis;
        private readonly ILogger<RedisSlackTeamRepository> _logger;
        private IDatabase _db;
        private string _server;

        public VerifiedPLEntriesRepository(ConnectionMultiplexer redis, IOptions<RedisOptions> redisOptions, ILogger<RedisSlackTeamRepository> logger)
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

    public record VerifiedPLEntry(
        int EntryId, 
        long TeamId, 
        long TeamCode, 
        string TeamName, 
        int PlayerId, 
        int PlayerCode, 
        string PlayerWebName,
        string PlayerFullName,
        SelfOwnershipStats SelfOwnershipStats = null
    );

    public record SelfOwnershipStats(int WeekCount, int TotalPoints, int Gameweek);
    
    public interface IVerifiedPLEntriesRepository
    {
        Task Insert(VerifiedPLEntry entry);
        Task<IEnumerable<VerifiedPLEntry>> GetAllVerifiedPLEntries();
        Task<VerifiedPLEntry> GetVerifiedPLEntry(int entryId);
        
        Task Delete(int entryId);
        Task DeleteAll();
        Task UpdateStats(int entryId, SelfOwnershipStats selfOwnershipStats);
    }
}