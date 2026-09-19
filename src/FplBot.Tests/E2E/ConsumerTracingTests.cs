using System.Diagnostics;
using Discord.Net.Endpoints;
using FplBot.Hosting;

namespace FplBot.Tests.E2E;

[Collection("App")]
public class ConsumerTracingTests(AppFixture fixture)
{
    [Fact]
    public async Task AConsumedMessageGetsASpanUnderTheOwningServicesSource()
    {
        List<(string Source, string Name)> started = [];
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == FplBotDiagnostics.SourceNameFor(FplBotService.EventHandlers),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity =>
            {
                lock (started) started.Add((activity.Source.Name, activity.OperationName));
            }
        };
        ActivitySource.AddActivityListener(listener);

        await fixture.AskSlackbot("<@UREFQD887> player haaland");

        await AppFixture.WaitUntil(() =>
        {
            lock (started)
                return Task.FromResult(started.Any(s => s.Source == FplBotDiagnostics.SourceNameFor(FplBotService.EventHandlers)));
        }, "No span was started under the EventHandlers source");
    }

    [Fact]
    public async Task ASlackAppMentionTagsTeamAndChannelOnTheSpan()
    {
        var teamId = await fixture.InstallSlackbot();

        List<Activity> started = [];
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity =>
            {
                lock (started) started.Add(activity);
            }
        };
        ActivitySource.AddActivityListener(listener);

        await fixture.AskSlackbot(teamId, "C0DEV000001", "<@UREFQD887> player haaland");

        await AppFixture.WaitUntil(() =>
        {
            lock (started)
                return Task.FromResult(started.Any(a => (string?)a.GetTagItem(FplBotDiagnostics.TeamIdTag) == teamId));
        });
    }

    [Fact]
    public async Task AConsumerSpanIsTaggedWithTheTeamFromTheMessage()
    {
        var teamId = await fixture.InstallSlackbot();

        List<Activity> started = [];
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == FplBotDiagnostics.SourceNameFor(FplBotService.EventHandlers),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity =>
            {
                lock (started) started.Add(activity);
            }
        };
        ActivitySource.AddActivityListener(listener);

        await fixture.AskSlackbot(teamId, "C0DEV000001", "<@UREFQD887> player haaland");

        await AppFixture.WaitUntil(() =>
        {
            lock (started)
                return Task.FromResult(started.Any(a => (string?)a.GetTagItem(FplBotDiagnostics.TeamIdTag) == teamId));
        });
    }

    [Fact]
    public async Task ADiscordInteractionGetsAWebhookSpanFromTheDiscordFramework()
    {
        List<(string Source, string Name)> started = [];
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == DiscordDiagnostics.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity =>
            {
                lock (started) started.Add((activity.Source.Name, activity.OperationName));
            }
        };
        ActivitySource.AddActivityListener(listener);

        await fixture.AskDiscord("help");

        (string, string)[] snapshot;
        lock (started) snapshot = [.. started];

        Assert.Contains((DiscordDiagnostics.ActivitySourceName, "DiscordWebhook"), snapshot);
    }
}
