using Bogus;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Domain;
using FplBot.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.E2E.Discord.DiscordSlashCommands;

[Collection("App")]
public class FollowSlashCommandHandlerTests(AppFixture fixture)
{
    private static readonly Faker Faker = new();

    private static int NewLeagueId() => Faker.Random.Int(100_000, 999_999);

    private static string ChannelOf(Installation installation) => installation.ChannelSubscriptions.First().ChannelId;

    private void SetLeagueFound(int leagueId, string name)
    {
        var leagueClient = fixture.Services.GetRequiredService<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(leagueId, A<int>._, A<bool>._))
            .Returns(new ClassicLeague { Properties = new ClassicLeagueProperties { Name = name } });
    }

    private void SetLeagueNotFound(int leagueId)
    {
        var leagueClient = fixture.Services.GetRequiredService<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(leagueId, A<int>._, A<bool>._)).Returns((ClassicLeague?)null);
    }

    [Fact]
    public async Task LeagueNotFound_RespondsWithError()
    {
        var leagueId = NewLeagueId();
        SetLeagueNotFound(leagueId);

        var (token, _) = await fixture.AskDiscord("follow", optionValue: leagueId.ToString());
        var followup = await fixture.DiscordCapture.WaitForFollowupAsync(token);

        Assert.Contains($"Could not find a classic league of id '{leagueId}'", followup.Description);
    }

    [Fact]
    public async Task NoExistingSubscription_CreatesOneAndSubscribesToAll()
    {
        var installedGuild = await fixture.SeedGuildInstallation();
        var leagueId = NewLeagueId();
        SetLeagueFound(leagueId, "Test League");

        var (token, _) = await fixture.AskDiscord("follow", optionValue: leagueId.ToString(),
            guildId: installedGuild.Id, channelId: Guid.NewGuid().ToString("N"));
        var followup = await fixture.DiscordCapture.WaitForFollowupAsync(token);

        Assert.Contains("Now following the 'Test League' FPL league", followup.Description);
        Assert.Contains("Auto-subbed to all events", followup.Description);
    }

    [Fact]
    public async Task ExistingSubscription_UpdatesLeague()
    {
        var seeded = await fixture.SeedGuildInstallation();
        var leagueId = NewLeagueId();
        SetLeagueFound(leagueId, "Other League");

        var (token, _) = await fixture.AskDiscord("follow", optionValue: leagueId.ToString(), guildId: seeded.Id, channelId: ChannelOf(seeded));
        var followup = await fixture.DiscordCapture.WaitForFollowupAsync(token);

        Assert.Contains("Now following the 'Other League' FPL league", followup.Description);
    }
}
