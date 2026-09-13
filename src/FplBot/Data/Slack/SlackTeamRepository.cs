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
        var team = await GetTeam(teamId);
        return ToDomain(team);
    }

    public static SlackInstallation ToDomain(SlackTeam team)
    {
        var channels = new List<SlackChannelSubscription>();

        if (!string.IsNullOrEmpty(team.FplBotSlackChannel))
        {
            var leagueId = team.FplbotLeagueId is { } id ? new ClassicLeagueId(id) : null;
            var events = team.Subscriptions.Select(ToDomainEvent);
            channels.Add(SlackChannelSubscription.FromStorage(team.FplBotSlackChannel, leagueId, events));
        }

        return SlackInstallation.Load(team.TeamId!, team.TeamName, team.AccessToken ?? string.Empty, channels, team.PendingRemoval ?? false);
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

    public async Task<SlackTeam?> FindByTeamId(string teamId)
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

        if (team.Subscriptions != null)
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
    }

    public async Task<SlackInstallation?> FindInstallationByTeamId(string teamId)
    {
        var team = await FindByTeamId(teamId);
        return team is null ? null : ToDomain(team);
    }

    public async Task UpdateLeagueId(string teamId, long newLeagueId)
    {
        if(string.IsNullOrEmpty(teamId))
            throw new ArgumentNullException(nameof(teamId));

        if(newLeagueId == 0)
            throw new ArgumentNullException(nameof(newLeagueId));

        await _db.HashSetAsync(FromTeamIdToTeamKey(teamId), [new HashEntry(_leagueField, newLeagueId)]);
    }

    public async Task UpdateChannel(string teamId, string newChannel)
    {
        if(string.IsNullOrEmpty(teamId))
            throw new ArgumentNullException(nameof(teamId));

        if(string.IsNullOrEmpty(newChannel))
            throw new ArgumentNullException(nameof(newChannel));

        await _db.HashSetAsync(FromTeamIdToTeamKey(teamId), [new HashEntry(_channelField, newChannel)]);
    }

    public async Task DeleteByTeamId(string teamId)
    {
        if(string.IsNullOrEmpty(teamId))
            throw new ArgumentNullException(nameof(teamId));

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

    public async Task<IEnumerable<SlackTeam>> GetAllTeams()
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

    public async Task UpdateSubscriptions(string teamId, IEnumerable<EventSubscription> subscriptions)
    {
        if(string.IsNullOrEmpty(teamId))
            throw new ArgumentNullException(nameof(teamId));

        await _db.HashSetAsync(FromTeamIdToTeamKey(teamId), [new HashEntry(_subscriptionsField, string.Join(" ", subscriptions))
        ]);
    }
}
