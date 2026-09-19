using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.E2E.Discord.DiscordSlashCommands;

[Collection("App")]
public class StandingsSlashCommandHandlerTests(AppFixture fixture) : IAsyncLifetime
{
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
    public async Task WhenChannelFollowsALeague_PostsStandingsForTheCurrentGameweek()
    {
        const int gameweekId = 7;
        const int leagueId = 12345;

        var installedGuild = await fixture.SeedGuildInstallation(leagueId);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var globalSettingsClient = fixture.Services.GetRequiredService<IGlobalSettingsClient>();
        A.CallTo(() => globalSettingsClient.GetGlobalSettings()).Returns(new GlobalSettings
        {
            Gameweeks =
            [
                new()
                {
                    Id = gameweekId,
                    Name = $"Gameweek {gameweekId}",
                    IsCurrent = true
                }
            ]
        });

        var leagueClient = fixture.Services.GetRequiredService<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(leagueId, A<int>._, A<bool>._)).Returns(new ClassicLeague
        {
            Properties = new ClassicLeagueProperties
            {
                Name = "Test League",
                StartEvent = 1
            },
            Standings = new ClassicLeagueStandings
            {
                Entries = [],
                HasNext = false
            }
        });

        await fixture.AskDiscord("standings", guildId: installedGuild.Id, channelId: channelId);

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("Standings", msg.Description);
        Assert.Contains($"Gameweek {gameweekId}", msg.Description);
    }

    [Fact]
    public async Task WhenChannelFollowsNoLeague_RespondsWithAWarning()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        var (token, _) = await fixture.AskDiscord("standings", guildId: installedGuild.Id, channelId: channelId);
        var followup = await fixture.DiscordCapture.WaitForFollowupAsync(token);

        Assert.Contains("isn't following an FPL league", followup.Description);
        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.DiscordCapture.AnyMessage(channelId));
    }
}
