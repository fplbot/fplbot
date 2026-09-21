using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace FplBot.Tests.E2E.Discord;

// Stubs DiscordClient.GuildGet's HTTP call (GET /api/v10/guilds/{guildId}?with_counts=true) for
// tests exercising RefreshGuildMemberCountHandler - a per-guild count, or a failure, is set up
// per test and read back from the request URL, mirroring CapturingSlackClient's per-channel
// controls for the Slack side of the same feature.
public partial class StubDiscordGuildGet : HttpMessageHandler
{
    private readonly ConcurrentDictionary<string, int> _memberCounts = new();
    private readonly ConcurrentDictionary<string, HttpStatusCode> _failing = new();

    public void SetApproximateMemberCount(string guildId, int count) => _memberCounts[guildId] = count;

    public void FailGuild(string guildId, HttpStatusCode status) => _failing[guildId] = status;

    public void Reset()
    {
        _memberCounts.Clear();
        _failing.Clear();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var match = GuildIdPattern().Match(request.RequestUri!.AbsolutePath);
        var guildId = match.Groups[1].Value;

        if (_failing.TryGetValue(guildId, out var status))
        {
            return Task.FromResult(new HttpResponseMessage(status));
        }

        var count = _memberCounts.GetValueOrDefault(guildId, 0);
        var body = $$"""{"id":"{{guildId}}","approximate_member_count":{{count}}}""";
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
    }

    [GeneratedRegex(@"/guilds/([^/]+)")]
    private static partial Regex GuildIdPattern();
}
