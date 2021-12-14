using FplBot.Data.Slack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Hosting;
using StackExchange.Redis;

namespace FplBot.WebApi.Slack.Data;

public class TokenStore : ITokenStore
{
    private string _accessTokenField = "accessToken";
    private string _channelField = "fplchannel";
    private string _leagueField = "fplleagueId";
    private string _teamNameField = "teamName";
    private string _teamIdField = "teamId";
    private string _subscriptionsField = "subscriptions";


    private readonly IConnectionMultiplexer _redis;
    private IDatabase _db;
    private string _server;
    private readonly ILogger<TokenStore> _logger;

    public TokenStore(IConnectionMultiplexer redis, IOptions<SlackRedisOptions> redisOptions, ILogger<TokenStore> logger)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
        _server = redisOptions.Value.GetRedisServerHostAndPort;
        _logger = logger;
    }

    public async Task Insert(Workspace workspace)
    {
        await Insert(new SlackTeam
        {
            TeamId = workspace.TeamId,
            TeamName = workspace.TeamName,
            AccessToken = workspace.Token
        });
    }

    public async Task Insert(SlackTeam slackTeam)
    {
        var hashEntries = new List<HashEntry>
        {
            new HashEntry(_accessTokenField, slackTeam.AccessToken),
            new HashEntry(_teamNameField, slackTeam.TeamName),
            new HashEntry(_teamIdField, slackTeam.TeamId)
        };

        if (!string.IsNullOrEmpty(slackTeam.FplBotSlackChannel))
        {
            hashEntries.Add(new HashEntry(_channelField, slackTeam.FplBotSlackChannel));
        }

        if (slackTeam.FplbotLeagueId > 0)
        {
            hashEntries.Add(new HashEntry(_leagueField, slackTeam.FplbotLeagueId));
        }

        if (slackTeam.Subscriptions != null)
        {
            var hashEntry = new HashEntry(_subscriptionsField, string.Join(" ", slackTeam.Subscriptions));
            hashEntries.Add(hashEntry);
        }

        await _db.HashSetAsync(FromTeamIdToTeamKey(slackTeam.TeamId), hashEntries.ToArray());
    }

    public async Task<Workspace> Delete(string teamId)
    {
        var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromTeamIdToTeamKey("*"));

        foreach (var key in allTeamKeys)
        {
            var fetchedTeamData = await _db.HashGetAsync(key, new RedisValue[] {_teamIdField, _teamNameField, _accessTokenField});
            if (fetchedTeamData[0] == teamId)
            {
                var workspace = new Workspace(TeamId: fetchedTeamData[0], TeamName: fetchedTeamData[1], Token: fetchedTeamData[2]);
                await _db.KeyDeleteAsync(key);
                return workspace;
            }

        }

        return null;
    }

    private static string FromTeamIdToTeamKey(string teamId)
    {
        return $"TeamId-{teamId}";
    }
}
