using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.E2E.Discord;

[Collection("App")]
public class DiscordGameweekFinishedHandlerTests(AppFixture fixture) : IAsyncLifetime
{
    // GetGlobalSettings() is parameterless, so overriding it below mutates the shared fake for
    // every other test in the collection unless restored — capture whatever was configured
    // before this test touches it, and put it back afterward.
    private GlobalSettings? _originalGlobalSettings;

    public async ValueTask InitializeAsync()
    {
        fixture.DiscordCapture.Reset();
        await fixture.FlushRedisAsync();

        _originalGlobalSettings = await fixture.Services.GetRequiredService<IGlobalSettingsClient>().GetGlobalSettings();
    }

    public ValueTask DisposeAsync()
    {
        A.CallTo(() => fixture.Services.GetRequiredService<IGlobalSettingsClient>().GetGlobalSettings()).Returns(_originalGlobalSettings);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task WhenChannelSubscribedToStandingsWithLeague_PostsStandings()
    {
        const int gameweekId = 7;
        const int leagueId = 12345;

        var installedGuild = await fixture.SeedGuildInstallation(leagueId, [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var globalSettingsClient = fixture.Services.GetRequiredService<IGlobalSettingsClient>();
        A.CallTo(() => globalSettingsClient.GetGlobalSettings()).Returns(new GlobalSettings
        {
            Gameweeks = new List<Gameweek> { new() { Id = gameweekId, Name = $"Gameweek {gameweekId}" } }
        });

        var leagueClient = fixture.Services.GetRequiredService<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(leagueId, A<int>._, A<bool>._)).Returns(new ClassicLeague
        {
            Properties = new ClassicLeagueProperties { Name = "Test League", StartEvent = 1 },
            Standings = new ClassicLeagueStandings { Entries = new List<ClassicLeagueEntry>(), HasNext = false }
        });

        await fixture.Bus.Publish(new GameweekFinished(new FinishedGameweek(gameweekId)), TestContext.Current.CancellationToken);

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("Gameweek finished", msg.Title);
    }

    [Fact]
    public async Task WhenChannelNotSubscribedToStandings_DoesNotPost()
    {
        var installedGuild = await fixture.SeedGuildInstallation(12345, [EventSubscription.PriceChanges]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new GameweekFinished(new FinishedGameweek(7)), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.DiscordCapture.WaitForMessageAsync(channelId, TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task PublishStandingsToDiscordGuild_PostsStandingsRegardlessOfSubscription()
    {
        const int gameweekId = 7;
        const int leagueId = 12345;

        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var globalSettingsClient = fixture.Services.GetRequiredService<IGlobalSettingsClient>();
        A.CallTo(() => globalSettingsClient.GetGlobalSettings()).Returns(new GlobalSettings
        {
            Gameweeks = new List<Gameweek> { new() { Id = gameweekId, Name = $"Gameweek {gameweekId}" } }
        });

        var leagueClient = fixture.Services.GetRequiredService<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(leagueId, A<int>._, A<bool>._)).Returns(new ClassicLeague
        {
            Properties = new ClassicLeagueProperties { Name = "Test League", StartEvent = 1 },
            Standings = new ClassicLeagueStandings { Entries = new List<ClassicLeagueEntry>(), HasNext = false }
        });

        await fixture.Bus.Publish(new PublishStandingsToDiscordGuild(installedGuild.Id, channelId, leagueId, gameweekId), TestContext.Current.CancellationToken);

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("Gameweek finished", msg.Title);
    }
}
