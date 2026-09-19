using System.Diagnostics;
using Discord.Net.Endpoints;
using FplBot.Hosting;

namespace FplBot.Tests.E2E.Discord;

[Collection("App")]
public class DiscordInteractionDiagnosticsTests(AppFixture fixture)
{
    [Fact]
    public async Task ASlashCommandTagsTeamAndChannelOnTheSpan()
    {
        var guildId = Guid.NewGuid().ToString("N");
        var channelId = Guid.NewGuid().ToString("N");

        List<Activity> stopped = [];
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == DiscordDiagnostics.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                lock (stopped) stopped.Add(activity);
            }
        };
        ActivitySource.AddActivityListener(listener);

        await fixture.AskDiscord("help", guildId: guildId, channelId: channelId);

        Activity[] snapshot;
        lock (stopped) snapshot = [.. stopped];
        var webhook = Assert.Single(snapshot, a => a.OperationName == "DiscordWebhook");
        Assert.Equal(guildId, webhook.GetTagItem(DiscordDiagnostics.GuildIdTag));
        Assert.Equal(channelId, webhook.GetTagItem(DiscordDiagnostics.ChannelIdTag));
        Assert.Equal(guildId, webhook.GetTagItem(FplBotDiagnostics.TeamIdTag));
        Assert.Equal(channelId, webhook.GetTagItem(FplBotDiagnostics.ChannelIdTag));
    }
}
