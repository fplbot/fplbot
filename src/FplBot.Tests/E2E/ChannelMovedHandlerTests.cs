using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E;

[Collection("App")]
public class ChannelMovedHandlerTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.FlushRedisAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SlackChannelMoved_IsLogged()
    {
        var teamId = "T" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        await fixture.Bus.Publish(new SlackChannelMoved(teamId, "C0OLD00001", "C0NEW00001"),
            TestContext.Current.CancellationToken);

        await AppFixture.WaitUntil(() =>
            Task.FromResult(fixture.LogCapture.Contains(teamId, "C0OLD00001", "C0NEW00001")));
    }

    [Fact]
    public async Task DiscordChannelMoved_IsLogged()
    {
        var guildId = Guid.NewGuid().ToString("N");

        await fixture.Bus.Publish(new DiscordChannelMoved(guildId, "111111111111111111", "222222222222222222"),
            TestContext.Current.CancellationToken);

        await AppFixture.WaitUntil(() =>
            Task.FromResult(fixture.LogCapture.Contains(guildId, "111111111111111111", "222222222222222222")));
    }
}
