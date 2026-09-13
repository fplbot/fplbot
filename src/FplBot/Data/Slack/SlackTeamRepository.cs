using FplBot.Domain;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FplBot.Data.Slack;

public class SlackTeamRepository : ISlackTeamRepository
{
    private readonly ILogger<SlackTeamRepository> _logger;

    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly string _server;

    private readonly string _accessTokenField = "accessToken";
    private readonly string _teamNameField = "teamName";
    private readonly string _teamIdField = "teamId";
    private readonly string _pendingRemovalField = "pendingRemoval";

    // Channel subscriptions: one hash per channel, keyed as SlackChannelSub-{teamId}-{channelId}.
    private readonly string _channelSubChannelIdField = "channelId";
    private readonly string _channelSubLeagueIdField = "leagueId";
    private readonly string _channelSubSubscriptionsField = "subscriptions";

    public SlackTeamRepository(IConnectionMultiplexer redis, IOptions<RedisOptions> redisOptions, ILogger<SlackTeamRepository> logger)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
        _server = redisOptions.Value.GetRedisServerHostAndPort;
        _logger = logger;
    }

    private record SlackTeamAccount(string TeamId, string TeamName, string? AccessToken, bool? PendingRemoval);

    private async Task<SlackTeamAccount> GetAccount(string teamId)
    {
        var fetched = await _db.HashGetAsync(FromTeamIdToTeamKey(teamId), [_accessTokenField, _teamNameField, _pendingRemovalField]);

        return new SlackTeamAccount(
            teamId,
            fetched[1]!,
            fetched[0],
            fetched[2].HasValue && (bool)fetched[2]);
    }

    public async Task<SlackInstallation> GetInstallation(string teamId)
    {
        if (!await _db.KeyExistsAsync(FromTeamIdToTeamKey(teamId)))
        {
            throw new KeyNotFoundException($"No Slack installation found for team id '{teamId}'");
        }

        var account = await GetAccount(teamId);
        return await LoadInstallation(account);
    }

    private async Task<SlackInstallation> LoadInstallation(SlackTeamAccount account)
    {
        var channels = await GetChannelSubscriptions(account.TeamId);
        return SlackInstallation.Load(account.TeamId, account.TeamName, account.AccessToken ?? string.Empty, channels, account.PendingRemoval ?? false);
    }

    private static FplEvent ToDomainEvent(EventSubscription e) => Enum.Parse<FplEvent>(e.ToString());

    private List<EventSubscription> GetSubscriptions(string teamId, RedisValue fetchedTeamData)
    {
        if (!fetchedTeamData.HasValue)
        {
            return [];
        }

        (var subs, var unableToParse) = fetchedTeamData.ToString().ParseSubscriptionString(delimiter: " ");

        if (unableToParse.Any())
        {
            _logger.LogError("Unable to parse events for team {team}: {unableToParse}", teamId, string.Join(", ", unableToParse));
        }

        return subs.ToList<EventSubscription>();
    }

    public async Task Save(SlackInstallation installation)
    {
        var account = new SlackTeamAccount(installation.TeamId, installation.TeamName, installation.Token, installation.PendingRemoval);
        await SaveAccount(account);

        foreach (var channel in installation.ChannelSubscriptions)
        {
            await SaveChannelSubscription(installation.TeamId, channel);
        }
    }

    private async Task SaveAccount(SlackTeamAccount account)
    {
        var hashEntries = new List<HashEntry>
        {
            new HashEntry(_accessTokenField, account.AccessToken),
            new HashEntry(_teamNameField, account.TeamName),
            new HashEntry(_teamIdField, account.TeamId)
        };

        if (account.PendingRemoval.HasValue)
        {
            hashEntries.Add(new HashEntry(_pendingRemovalField, account.PendingRemoval.Value));
        }

        await _db.HashSetAsync(FromTeamIdToTeamKey(account.TeamId), hashEntries.ToArray());
    }

    public async Task<SlackInstallation?> FindInstallationByTeamId(string teamId)
    {
        var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromTeamIdToTeamKey("*"));

        foreach (var key in allTeamKeys)
        {
            var storedTeamId = FromKeyToTeamId(key.ToString());
            if (string.Compare(storedTeamId, teamId, StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                continue;
            }

            var account = await GetAccount(storedTeamId);
            return await LoadInstallation(account);
        }

        return null;
    }

    public async Task Delete(SlackInstallation installation)
    {
        var teamId = installation.TeamId;

        var channelIds = await _db.SetMembersAsync(ToChannelSubIndexKey(teamId));
        foreach (var channelId in channelIds)
        {
            await _db.KeyDeleteAsync(FromTeamAndChannelToChannelSubKey(teamId, channelId.ToString()));
        }
        await _db.KeyDeleteAsync(ToChannelSubIndexKey(teamId));

        await _db.KeyDeleteAsync(FromTeamIdToTeamKey(teamId));
    }

    public async Task<IEnumerable<string>> GetTokens()
    {
        var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromTeamIdToTeamKey("*"));
        var tokens = new List<string>();
        foreach (var key in allTeamKeys)
        {
            var token = await _db.HashGetAsync(key,_accessTokenField);
            tokens.Add(token.ToString() ?? string.Empty);
        }
        return tokens.Select(t => t.ToString());
    }

    public async Task<string?> GetTokenByTeamId(string teamId)
    {
        return await _db.HashGetAsync(FromTeamIdToTeamKey(teamId), _accessTokenField);
    }
    private static string FromTeamIdToTeamKey(string teamId)
    {
        return $"TeamId-{teamId}";
    }

    private static string FromKeyToTeamId(string key)
    {
        return key.Substring(key.IndexOf('-') + 1);
    }

    public async Task<IEnumerable<SlackInstallation>> GetAllInstallations()
    {
        var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromTeamIdToTeamKey("*"));
        var installations = new List<SlackInstallation>();
        foreach (var key in allTeamKeys)
        {
            var teamId = FromKeyToTeamId(key.ToString());
            var account = await GetAccount(teamId);
            installations.Add(await LoadInstallation(account));
        }

        return installations;
    }

    private async Task SaveChannelSubscription(string teamId, SlackChannelSubscription channel)
    {
        var key = FromTeamAndChannelToChannelSubKey(teamId, channel.ChannelId);
        var subscriptions = channel.Events.Current.Select(ToStorageEvent);

        var hashEntries = new List<HashEntry>
        {
            new HashEntry(_teamIdField, teamId),
            new HashEntry(_channelSubChannelIdField, channel.ChannelId),
            new HashEntry(_channelSubSubscriptionsField, string.Join(" ", subscriptions))
        };

        if (channel.FollowedLeagueId is { } leagueId)
        {
            hashEntries.Add(new HashEntry(_channelSubLeagueIdField, (int)leagueId.Value));
        }

        await _db.HashSetAsync(key, hashEntries.ToArray());
        await _db.SetAddAsync(ToChannelSubIndexKey(teamId), channel.ChannelId);
    }

    // Reads the exact set of channel ids this team has saved, then fetches each channel's hash by
    // its exact key. Deliberately avoids a KEYS pattern scan on "SlackChannelSub-{teamId}-*": that
    // glob also matches OTHER teams whose id happens to start with this team's id plus a dash
    // (e.g. scanning for "DEV-SLACK" would also match "DEV-SLACK-2", "DEV-SLACK-BARE", ...).
    public async Task<IEnumerable<SlackChannelSubscription>> GetChannelSubscriptions(string teamId)
    {
        var channelIds = await _db.SetMembersAsync(ToChannelSubIndexKey(teamId));
        var result = new List<SlackChannelSubscription>();

        foreach (var channelIdValue in channelIds)
        {
            var channelId = channelIdValue.ToString();
            var fetched = await _db.HashGetAsync(FromTeamAndChannelToChannelSubKey(teamId, channelId), [_channelSubChannelIdField, _channelSubLeagueIdField, _channelSubSubscriptionsField]);
            int? leagueId = fetched[1].HasValue ? int.Parse((string)fetched[1]!) : null;
            var subs = GetSubscriptions(teamId, fetched[2]);
            var domainLeagueId = leagueId is { } id ? new ClassicLeagueId(id) : null;
            result.Add(SlackChannelSubscription.Load(channelId, domainLeagueId, subs.Select(ToDomainEvent)));
        }

        return result;
    }

    private static string FromTeamAndChannelToChannelSubKey(string teamId, string channelId) =>
        $"SlackChannelSub-{teamId}-{channelId}";

    private static string ToChannelSubIndexKey(string teamId) =>
        $"SlackChannelSubIndex-{teamId}";

    private static EventSubscription ToStorageEvent(FplEvent e) => Enum.Parse<EventSubscription>(e.ToString());
}
