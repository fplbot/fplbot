using System.Diagnostics;
using FakeItEasy;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Fpl.Search;
using Fpl.Search.Indexing;
using FplBot.Core.RecurringActions;
using FplBot.Hosting;
using FplBot.WebApi.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FplBot.Tests.UnitTests;

public class RecurringActionTracingTests
{
    [Fact]
    public async Task IndexerRecurringActionStartsASpan()
    {
        var action = new IndexerRecurringAction(
            A.Fake<IIndexingService>(),
            NullLogger<IndexerRecurringAction>.Instance,
            Options.Create(new SearchOptions
            {
                IndexUri = "http://localhost:9200",
                Username = "u",
                Password = "p",
                EntriesIndex = "entries",
                LeaguesIndex = "leagues",
                AnalyticsIndex = "analytics",
                IndexingCron = "0 0 * * * *",
                ShouldIndexEntries = false,
                ShouldIndexLeagues = false
            }));

        var started = await CaptureSpans(() => action.Process(CancellationToken.None));

        Assert.Contains((FplBotDiagnostics.SourceNameFor(FplBotService.SearchIndexer), nameof(IndexerRecurringAction)), started);
    }

    [Fact]
    public async Task GuildMemberCountCheckerStartsASpan()
    {
        var action = new GuildMemberCountChecker(ScopeFactoryPublishing(A.Fake<IPublishEndpoint>()));

        var started = await CaptureSpans(() => action.Process(CancellationToken.None));

        Assert.Contains((FplBotDiagnostics.SourceNameFor(FplBotService.WebApi), nameof(GuildMemberCountChecker)), started);
    }

    [Fact]
    public async Task SlackChannelMemberCountCheckerStartsASpan()
    {
        var action = new SlackChannelMemberCountChecker(ScopeFactoryPublishing(A.Fake<IPublishEndpoint>()));

        var started = await CaptureSpans(() => action.Process(CancellationToken.None));

        Assert.Contains((FplBotDiagnostics.SourceNameFor(FplBotService.WebApi), nameof(SlackChannelMemberCountChecker)), started);
    }

    private static IServiceScopeFactory ScopeFactoryPublishing(IPublishEndpoint publishEndpoint)
    {
        var services = new ServiceCollection();
        services.AddSingleton(publishEndpoint);
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private static async Task<List<(string Source, string Name)>> CaptureSpans(Func<Task> act)
    {
        List<(string Source, string Name)> started = [];
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith(FplBotDiagnostics.ActivitySourceName),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => started.Add((activity.Source.Name, activity.OperationName))
        };
        ActivitySource.AddActivityListener(listener);

        await act();

        return started;
    }
}
