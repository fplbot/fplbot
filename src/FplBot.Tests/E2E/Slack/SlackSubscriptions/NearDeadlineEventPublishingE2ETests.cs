using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Helpers;
using Fpl.EventPublishers.States;
using FplBot.Domain;
using FplBot.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FplBot.Tests.E2E.Slack.SlackSubscriptions;

[Collection("App")]
public class NearDeadlineEventPublishingE2ETests(AppFixture fixture) : IAsyncLifetime
{
    private string _channel = null!;

    public async ValueTask InitializeAsync()
    {
        fixture.SlackCapture.Reset();
        await fixture.FlushRedisAsync();
        _channel = "#deadlines-" + Guid.NewGuid().ToString("N")[..8];
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, _channel, FplEvent.Deadlines);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task OnlyPublishesOnceForFirstGameweek()
    {
        var gameweek1 = new Gameweek { IsCurrent = false, IsNext = true, Deadline = new DateTime(2021, 8, 15, 10, 0, 0) };
        var gameweek2 = new Gameweek { IsCurrent = false, IsNext = false, Deadline = new DateTime(2021, 8, 22, 10, 0, 0) };
        var globalSettings = new GlobalSettings { Gameweeks = new List<Gameweek> { gameweek1, gameweek2 } };
        var fakeSettingsClient = GlobalSettingsClientBuilder.Returning(globalSettings);
        var dateTimeUtils = new DateTimeUtils { NowUtcOverride = new DateTime(2021, 8, 14, 10, 0, 0) };
        var handler = CreateMonitor(fakeSettingsClient, dateTimeUtils);

        await handler.EveryMinuteTick();

        // TwentyFourHoursToDeadline produces two Slack messages: the deadline notice and its
        // threaded fixtures-for-gameweek reply.
        var msg = await fixture.SlackCapture.WaitForMessageAsync(_channel);
        await fixture.SlackCapture.WaitForMessageAsync(_channel);
        Assert.Contains("deadline in 24 hours", msg.Text);
    }

    [Fact]
    public async Task OnlyPublishesOnceForSecondGameweekWhenFirstGameweekIsCurrent()
    {
        var gameweek1 = new Gameweek { IsCurrent = true, IsNext = false, Deadline = new DateTime(2021, 8, 15, 10, 0, 0) };
        var gameweek2 = new Gameweek { IsCurrent = false, IsNext = true, Deadline = new DateTime(2021, 8, 22, 10, 0, 0) };
        var globalSettings = new GlobalSettings { Gameweeks = new List<Gameweek> { gameweek1, gameweek2 } };
        var fakeSettingsClient = GlobalSettingsClientBuilder.Returning(globalSettings);
        var dateTimeUtils = new DateTimeUtils { NowUtcOverride = new DateTime(2021, 8, 21, 10, 0, 0) };
        var handler = CreateMonitor(fakeSettingsClient, dateTimeUtils);

        await handler.EveryMinuteTick();

        var msg = await fixture.SlackCapture.WaitForMessageAsync(_channel);
        await fixture.SlackCapture.WaitForMessageAsync(_channel);
        Assert.Contains("deadline in 24 hours", msg.Text);
    }

    private NearDeadLineMonitor CreateMonitor(IGlobalSettingsClient settingsClient, DateTimeUtils dateTimeUtils) =>
        new(settingsClient, dateTimeUtils, fixture.Services.GetRequiredService<IServiceScopeFactory>(), A.Fake<ILogger<NearDeadLineMonitor>>());
}
