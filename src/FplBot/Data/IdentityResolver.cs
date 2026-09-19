using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;
using StackExchange.Redis;

namespace FplBot.Data;

public record ResolvedInstallation(ChatPlatform Platform, string ExternalId);

public record ResolvedSubscription(ChatPlatform Platform, string InstallationExternalId, string ChannelId);

public interface IIdentityResolver
{
    Task<ResolvedInstallation?> ResolveInstallation(InstallationId id);

    Task<ResolvedSubscription?> ResolveSubscription(SubscriptionId id);
}

// The internal ids address an installation or subscription without the caller knowing which chat
// platform it belongs to. Redis keeps the platform-shaped keys; these two reverse index entries are
// what turn an id from a URL back into them.
public class IdentityResolver(IConnectionMultiplexer redis) : IIdentityResolver
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<ResolvedInstallation?> ResolveInstallation(InstallationId id)
    {
        var entry = await _db.StringGetAsync($"InstallationId-{id.Value}");
        if (!entry.HasValue)
        {
            return null;
        }

        var parts = entry.ToString().Split(':', 2);
        return parts.Length == 2 && ToPlatform(parts[0]) is { } platform
            ? new ResolvedInstallation(platform, parts[1])
            : null;
    }

    public async Task<ResolvedSubscription?> ResolveSubscription(SubscriptionId id)
    {
        var entry = await _db.StringGetAsync($"SubId-{id.Value}");
        if (!entry.HasValue)
        {
            return null;
        }

        var parts = entry.ToString().Split(':', 3);
        return parts.Length == 3 && ToPlatform(parts[0]) is { } platform
            ? new ResolvedSubscription(platform, parts[1], parts[2])
            : null;
    }

    private static ChatPlatform? ToPlatform(string prefix) => prefix switch
    {
        "slack" => ChatPlatform.Slack,
        "discord" => ChatPlatform.Discord,
        _ => null
    };
}
