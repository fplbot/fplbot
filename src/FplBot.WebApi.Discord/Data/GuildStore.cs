using Discord.Net.Endpoints.Hosting;
using FplBot.Data.Discord;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FplBot.Discord.Data;

public class DiscordGuildStore : IGuildStore
{
    private readonly RedisValue _nameField = "name";
    private readonly RedisValue _guildIdField = "guildid";
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly string _server;
    private readonly ILogger<DiscordGuildStore> _logger;

    public DiscordGuildStore(IConnectionMultiplexer redis, IOptions<DiscordRedisOptions> redisOptions,
        ILogger<DiscordGuildStore> logger)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
        _server = redisOptions.Value.GetRedisServerHostAndPort;
        _logger = logger;
    }


    public async Task Insert(Guild guild)
    {
        var hashEntries = new List<HashEntry>
        {
            new HashEntry(_guildIdField, guild.Id), new HashEntry(_nameField, guild.Name)
        };
        await _db.HashSetAsync(FromGuildIdToGuildKey(guild.Id), hashEntries.ToArray());
    }

    public async Task<Guild> DeleteGuild(string guildId)
    {
        var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromGuildIdToGuildKey("Guild-*"));

        foreach (var key in allTeamKeys)
        {
            var fetchedTeamData = await _db.HashGetAsync(key, new[] { _guildIdField, _nameField });
            if (fetchedTeamData[0] == guildId)
            {
                await _db.KeyDeleteAsync(key);
                return new Guild(fetchedTeamData[0].ToString(), fetchedTeamData[1].ToString());
            }
        }

        return null;
    }

    private static string FromGuildIdToGuildKey(string guildId)
    {
        return $"Guild-{guildId}";
    }
}
