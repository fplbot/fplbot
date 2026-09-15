using FplBot.Domain;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FplBot.Data.Discord;

public class DiscordGuildRepository : IGuildRepository
{
    private readonly RedisValue _nameField = "name";
    private readonly RedisValue _guildIdField = "guildid";
    private readonly RedisValue _channelIdField = "channelid";
    private readonly RedisValue _leagueIdField = "leagueid";
    private readonly RedisValue _subscriptionsField = "subs";

    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly string _server;
    private readonly ILogger<DiscordGuildRepository> _logger;

    public DiscordGuildRepository(IConnectionMultiplexer redis, IOptions<RedisOptions> redisOptions, ILogger<DiscordGuildRepository> logger)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
        _server = redisOptions.Value.GetRedisServerHostAndPort;
        _logger = logger;
    }

    public async Task<IEnumerable<GuildRepoGuild>> GetAllGuilds()
    {
        var allKeys = _redis.GetServer(_server).Keys(pattern: FromGuildIdToGuildKey("*"));
        var guilds = new List<GuildRepoGuild>();
        foreach (var key in allKeys)
        {
            var teamId = FromKeyToGuildId(key);
            var fetchedTeamData = await _db.HashGetAsync(key, [_nameField]);
            guilds.Add(new GuildRepoGuild(teamId, fetchedTeamData[0].ToString() ?? string.Empty));
        }

        return guilds;
    }

    public async Task<IEnumerable<GuildFplSubscription>> GetAllGuildSubscriptions()
    {
        var allKeys = _redis.GetServer(_server).Keys(pattern: "GuildSubs-*-Channel-*");
        var guilds = new List<GuildFplSubscription>();
        foreach (var key in allKeys)
        {
            var guildId = FromKeyToGuildId(key);
            var fetchedTeamData = await _db.HashGetAsync(key, [_guildIdField, _channelIdField, _leagueIdField, _subscriptionsField
            ]);
            var subs = ParseSubscriptionString(fetchedTeamData[3].ToString(), " ");
            guilds.Add(new GuildFplSubscription(guildId, fetchedTeamData[1].ToString() ?? string.Empty, (int?)fetchedTeamData[2], subs));
        }

        return guilds;
    }

    public async Task<GuildFplSubscription> GetGuildSubscription(string guildId, string channelId)
    {
        var allKeys = _redis.GetServer(_server).Keys(pattern: FromGuildIdAndChannelToGuildChannelSubKey(guildId, channelId));
        var guilds = new List<GuildFplSubscription>();
        foreach (var key in allKeys)
        {
            var fetchedTeamData = await _db.HashGetAsync(key, [_guildIdField, _channelIdField, _leagueIdField, _subscriptionsField
            ]);
            var subs = ParseSubscriptionString(fetchedTeamData[3].ToString(), " ");
            guilds.Add(new GuildFplSubscription(guildId, fetchedTeamData[1].ToString() ?? string.Empty, (int?)fetchedTeamData[2], subs));
        }

        return guilds.FirstOrDefault()!;
    }

    public async Task DeleteGuildSubscription(string guildId, string channelId)
    {
        var allTeamKeys = _redis.GetServer(_server).Keys(pattern:FromGuildIdAndChannelToGuildChannelSubKey(guildId, channelId));

        foreach (var key in allTeamKeys)
        {
            await _db.KeyDeleteAsync(key);
        }
    }

    public async Task UpdateGuildSubscription(GuildFplSubscription guildSub)
    {
        await InsertGuildSubscription(guildSub);
    }

    public async Task InsertGuildSubscription(GuildFplSubscription guildSub)
    {
        var hashEntries = new List<HashEntry>
        {
            new HashEntry(_guildIdField, guildSub.GuildId),
            new HashEntry(_channelIdField, guildSub.ChannelId),
            new HashEntry(_subscriptionsField, string.Join(" ", guildSub.Subscriptions))
        };

        if (guildSub.LeagueId != null)
            hashEntries.Add(new HashEntry(_leagueIdField, guildSub.LeagueId));

        await _db.HashSetAsync(FromGuildIdAndChannelToGuildChannelSubKey(guildSub.GuildId, guildSub.ChannelId), hashEntries.ToArray());
    }

    public async Task DeleteGuild(string guildId)
    {
        await _db.KeyDeleteAsync(FromGuildIdToGuildKey(guildId));
    }

    private static string FromGuildIdToGuildKey(string guildId)
    {
        return $"Guild-{guildId}";
    }

    private static string FromGuildIdAndChannelToGuildChannelSubKey(string guildId, string channelId)
    {
        return $"GuildSubs-{guildId}-Channel-{channelId}";
    }

    private static string FromKeyToGuildId(string? key)
    {
        return key?.Split('-')[1] ?? string.Empty;
    }

    private static IEnumerable<EventSubscription> ParseSubscriptionString(string? subscriptionString, string delimiter)
    {
        var events = new List<EventSubscription>();
        var erroneous = new List<string>();

        if (string.IsNullOrWhiteSpace(subscriptionString))
        {
            return [];
        }

        var split = subscriptionString.Split(delimiter);
        foreach (var s in split)
        {
            var trimmed = s.Trim();
            if (Enum.TryParse(typeof(EventSubscription), trimmed, true, out var result))
            {
                events.Add((EventSubscription)result);
            }
            else
            {
                erroneous.Add(trimmed);
            }
        }

        return events;
    }

    public async Task<Installation> GetInstallation(string teamId)
    {
        var installation = await FindInstallationByTeamId(teamId);
        if (installation is null)
        {
            throw new KeyNotFoundException($"No Discord guild found for id '{teamId}'");
        }
        return installation;
    }

    public async Task<Installation?> FindInstallationByTeamId(string teamId)
    {
        var key = FromGuildIdToGuildKey(teamId);
        if (!await _db.KeyExistsAsync(key))
        {
            return null;
        }

        var fetched = await _db.HashGetAsync(key, [_nameField]);
        var channels = await GetChannelSubscriptions(teamId);
        return Installation.Load(teamId, fetched[0].ToString() ?? string.Empty, token: null, channels);
    }

    public async Task<IEnumerable<Installation>> GetAllInstallations()
    {
        var guilds = await GetAllGuilds();
        var subsByGuild = (await GetAllGuildSubscriptions()).ToLookup(s => s.GuildId);
        return guilds.Select(g => Installation.Load(g.Id, g.Name, token: null, subsByGuild[g.Id].Select(ToChannelSubscription))).ToList();
    }

    public async Task Save(Installation installation)
    {
        var storedChannelIds = (await GetAllGuildSubscriptions())
            .Where(s => s.GuildId == installation.Id)
            .Select(s => s.ChannelId)
            .ToHashSet();

        var hashEntries = new HashEntry[] { new(_guildIdField, installation.Id), new(_nameField, installation.Name) };
        await _db.HashSetAsync(FromGuildIdToGuildKey(installation.Id), hashEntries);

        var currentChannelIds = installation.ChannelSubscriptions.Select(c => c.ChannelId).ToHashSet();

        foreach (var channel in installation.ChannelSubscriptions)
        {
            await InsertGuildSubscription(ToGuildFplSubscription(installation.Id, channel));
        }

        foreach (var removedChannelId in storedChannelIds.Except(currentChannelIds))
        {
            await DeleteGuildSubscription(installation.Id, removedChannelId);
        }
    }

    public async Task Delete(Installation installation)
    {
        var subs = await GetChannelSubscriptions(installation.Id);
        foreach (var sub in subs)
        {
            await DeleteGuildSubscription(installation.Id, sub.ChannelId);
        }
        await DeleteGuild(installation.Id);
    }

    private async Task<IEnumerable<ChannelSubscription>> GetChannelSubscriptions(string teamId)
    {
        var allSubs = await GetAllGuildSubscriptions();
        return allSubs.Where(s => s.GuildId == teamId).Select(ToChannelSubscription).ToList();
    }

    private static ChannelSubscription ToChannelSubscription(GuildFplSubscription sub) =>
        ChannelSubscription.Load(sub.ChannelId, sub.LeagueId is { } id ? new ClassicLeagueId(id) : null, sub.Subscriptions.Select(ToDomainEvent));

    private static GuildFplSubscription ToGuildFplSubscription(string guildId, ChannelSubscription channel) =>
        new(guildId, channel.ChannelId, channel.FollowedLeagueId is { } leagueId ? (int)leagueId.Value : null, channel.Events.Current.Select(ToStorageEvent));

    private static FplEvent ToDomainEvent(EventSubscription e) => Enum.Parse<FplEvent>(e.ToString());
    private static EventSubscription ToStorageEvent(FplEvent e) => Enum.Parse<EventSubscription>(e.ToString());
}
