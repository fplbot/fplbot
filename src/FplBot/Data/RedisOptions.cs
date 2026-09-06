namespace FplBot.Data;

public class RedisOptions
{
    public required string REDIS_URL { get; set; }
    public string GetRedisPassword => RedisUri().UserInfo.Split(":")[1];
    public string GetRedisUsername => RedisUri().UserInfo.Split(":")[0];
    public string GetRedisServerHostAndPort => REDIS_URL.Split("@")[1];
    public string GetHost => GetRedisServerHostAndPort.Split(":")[0];
    private Uri? _uri;
    private Uri RedisUri() => _uri ??= new Uri(REDIS_URL);
}
