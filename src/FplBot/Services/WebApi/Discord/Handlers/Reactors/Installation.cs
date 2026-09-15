using Discord.Net.Endpoints.Hosting;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FplBot.Services.WebApi.Discord.Handlers.Reactors;

public class DiscordNetInstallationBridge : IGuildInstallationHandler
{
    private readonly RedisValue _nameField = "name";
    private readonly RedisValue _guildIdField = "guildid";
    private readonly IConnectionMultiplexer _redis;
    private readonly IPublishEndpoint _publisher;
    private readonly IDatabase _db;
    private readonly string _server;
    private readonly ILogger<DiscordNetInstallationBridge> _logger;

    public DiscordNetInstallationBridge(IConnectionMultiplexer redis, IOptions<RedisOptions> redisOptions,
        IPublishEndpoint publisher, ILogger<DiscordNetInstallationBridge> logger)
    {
        _redis = redis;
        _publisher = publisher;
        _db = _redis.GetDatabase();
        _server = redisOptions.Value.GetRedisServerHostAndPort;
        _logger = logger;
    }


    public async Task Install(Guild guild)
    {
        var hashEntries = new List<HashEntry>
        {
            new(_guildIdField, guild.Id), new HashEntry(_nameField, guild.Name)
        };
        await _db.HashSetAsync(FromGuildIdToGuildKey(guild.Id), hashEntries.ToArray());
        await _publisher.Publish(new AppInstalled(guild.Id, guild.Name, ChatPlatform.Discord));
    }

    public async Task Uninstall(string guildId)
    {
        var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromGuildIdToGuildKey("Guild-*"));

        foreach (var key in allTeamKeys)
        {
            var fetchedTeamData = await _db.HashGetAsync(key, [_guildIdField, _nameField]);
            if (fetchedTeamData[0] == guildId)
            {
                string? guildName = await _db.HashGetAsync(key, _nameField);
                await _db.KeyDeleteAsync(key);
                await _publisher.Publish(new AppUninstalled(guildId, guildName ?? "Unknown"));
                return;
            }
        }

    }

    private static string FromGuildIdToGuildKey(string guildId)
    {
        return $"Guild-{guildId}";
    }
}
