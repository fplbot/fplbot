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
    private readonly string _channelField = "fplchannel";
    private readonly string _leagueField = "fplleagueId";
    private readonly string _teamNameField = "teamName";
    private readonly string _teamIdField = "teamId";
    private readonly string _subscriptionsField = "subscriptions";
    private readonly string _pendingRemovalField = "pendingRemoval";

    // Channel subscriptions: one hash per channel, keyed as SlackChannelSub-{teamId}-{channelId} —
    // replaces the single scalar Channel/LeagueId/Subscriptions fields legacy SlackTeam stores per team.
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

    public async Task<SlackTeam> GetTeam(string teamId)
    {
        var fetchedTeamData = await _db.HashGetAsync(FromTeamIdToTeamKey(teamId), [_accessTokenField, _channelField, _leagueField, _teamNameField, _subscriptionsField, _pendingRemovalField
        ]);

        var team = new SlackTeam
        {
            AccessToken = fetchedTeamData[0],
            TeamName = fetchedTeamData[3]!,
            TeamId = teamId
        };

        if (fetchedTeamData[1].HasValue)
        {
            team.FplBotSlackChannel = fetchedTeamData[1];
        }

        if (fetchedTeamData[2].HasValue)
        {
            team.FplbotLeagueId = int.Parse((string)fetchedTeamData[2]!);
        }

        var subs = GetSubscriptions(teamId, fetchedTeamData[4]);

        team.Subscriptions = subs;
        team.PendingRemoval = fetchedTeamData[5].HasValue && (bool)fetchedTeamData[5];

        return team;
    }

    public async Task<SlackInstallation> GetInstallation(string teamId)
    {
        if (!await _db.KeyExistsAsync(FromTeamIdToTeamKey(teamId)))
        {
            throw new KeyNotFoundException($"No Slack installation found for team id '{teamId}'");
        }

        var team = await GetTeam(teamId);
        return await LoadInstallation(team);
    }

    private async Task<SlackInstallation> LoadInstallation(SlackTeam team)
    {
        var channels = await GetChannelSubscriptions(team.TeamId!);
        return SlackInstallation.Load(team.TeamId!, team.TeamName, team.AccessToken ?? string.Empty, channels, team.PendingRemoval ?? false);
    }

    public static SlackInstallation ToDomainV1(SlackTeam team) =>
        SlackInstallation.Load(
            team.TeamId,
            team.TeamName,
            team.AccessToken,
            string.IsNullOrEmpty(team.FplBotSlackChannel)
                ? []
                : [SlackChannelSubscription.Load(team.FplBotSlackChannel, team.FplbotLeagueId is { } id ? new ClassicLeagueId(id) : null, team.Subscriptions.Select(ToDomainEvent))],
            team.PendingRemoval ?? false);

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

    public async Task<SlackTeam?> FindTeamLegacyDoNotUse(string teamId)
    {
        var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromTeamIdToTeamKey("*"));

        foreach (var key in allTeamKeys)
        {
            var storedTeamId = FromKeyToTeamId(key.ToString());
            if (string.Compare(storedTeamId, teamId, StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                continue;
            }

            return await GetTeam(storedTeamId);
        }

        return null;
    }

    public async Task Save(SlackTeam team)
    {
        var hashEntries = new List<HashEntry>
        {
            new HashEntry(_accessTokenField, team.AccessToken),
            new HashEntry(_teamNameField, team.TeamName),
            new HashEntry(_teamIdField, team.TeamId)
        };

        if (!string.IsNullOrEmpty(team.FplBotSlackChannel))
        {
            hashEntries.Add(new HashEntry(_channelField, team.FplBotSlackChannel));
        }

        if (team.FplbotLeagueId > 0)
        {
            hashEntries.Add(new HashEntry(_leagueField, team.FplbotLeagueId));
        }

        if (team.Subscriptions.Any())
        {
            hashEntries.Add(new HashEntry(_subscriptionsField, string.Join(" ", team.Subscriptions)));
        }

        if (team.PendingRemoval.HasValue)
        {
            hashEntries.Add(new HashEntry(_pendingRemovalField, team.PendingRemoval.Value));
        }

        await _db.HashSetAsync(FromTeamIdToTeamKey(team.TeamId ?? string.Empty), hashEntries.ToArray());
    }

    public async Task Save(SlackInstallation installation)
    {
        await Save(SlackInstallationMapper.ToStorage(installation));

        foreach (var channel in installation.ChannelSubscriptions)
        {
            await SaveChannelSubscription(installation.TeamId, channel);
        }
    }

    public async Task<SlackInstallation?> FindInstallationByTeamId(string teamId)
    {
        var team = await FindTeamLegacyDoNotUse(teamId);
        return team is null ? null : await LoadInstallation(team);
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

    public async Task<IEnumerable<SlackTeam>> GetAllTeamsLegacyDoNotUse()
    {
        var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromTeamIdToTeamKey("*"));
        var teams = new List<SlackTeam>();
        foreach (var key in allTeamKeys)
        {
            var teamId = FromKeyToTeamId(key.ToString());

            var fetchedTeamData = await _db.HashGetAsync(key, [_accessTokenField, _channelField, _leagueField, _teamNameField, _subscriptionsField, _pendingRemovalField
            ]);

            var slackTeam = new SlackTeam
            {
                AccessToken = fetchedTeamData[0],
                TeamName = fetchedTeamData[3]!,
                TeamId = teamId
            };

            if (fetchedTeamData[1].HasValue)
            {
                slackTeam.FplBotSlackChannel = fetchedTeamData[1];
            }

            if (fetchedTeamData[2].HasValue)
            {
                slackTeam.FplbotLeagueId = int.Parse((string)fetchedTeamData[2]!);
            }

            var subs = GetSubscriptions(teamId, fetchedTeamData[4]);

            slackTeam.Subscriptions = subs;
            slackTeam.PendingRemoval = fetchedTeamData[5].HasValue && (bool)fetchedTeamData[5];
            teams.Add(slackTeam);
        }

        return teams;
    }

    public async Task<IEnumerable<SlackInstallation>> GetAllInstallations()
    {
        var teams = await GetAllTeamsLegacyDoNotUse();
        var installations = new List<SlackInstallation>();

        foreach (var team in teams)
        {
            installations.Add(await LoadInstallation(team));
        }

        return installations;
    }

    private async Task SaveChannelSubscription(string teamId, SlackChannelSubscription channel)
    {
        var record = ToRecord(teamId, channel);
        var key = FromTeamAndChannelToChannelSubKey(teamId, channel.ChannelId);

        var hashEntries = new List<HashEntry>
        {
            new HashEntry(_teamIdField, teamId),
            new HashEntry(_channelSubChannelIdField, record.ChannelId),
            new HashEntry(_channelSubSubscriptionsField, string.Join(" ", record.Subscriptions))
        };

        if (record.LeagueId.HasValue)
        {
            hashEntries.Add(new HashEntry(_channelSubLeagueIdField, record.LeagueId.Value));
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
            result.Add(ToDomainV2(new SlackChannelSubscriptionRecord(teamId, channelId, leagueId, subs)));
        }

        return result;
    }

    private static string FromTeamAndChannelToChannelSubKey(string teamId, string channelId) =>
        $"SlackChannelSub-{teamId}-{channelId}";

    private static string ToChannelSubIndexKey(string teamId) =>
        $"SlackChannelSubIndex-{teamId}";

    private static SlackChannelSubscriptionRecord ToRecord(string teamId, SlackChannelSubscription channel) =>
        new(teamId, channel.ChannelId, channel.FollowedLeagueId is { } id ? (int)id.Value : null, channel.Events.Current.Select(ToStorageEvent));

    private static SlackChannelSubscription ToDomainV2(SlackChannelSubscriptionRecord record)
    {
        var leagueId = record.LeagueId is { } id ? new ClassicLeagueId(id) : null;
        var events = record.Subscriptions.Select(ToDomainEvent);
        return SlackChannelSubscription.Load(record.ChannelId, leagueId, events);
    }

    private static EventSubscription ToStorageEvent(FplEvent e) => Enum.Parse<EventSubscription>(e.ToString());
}
