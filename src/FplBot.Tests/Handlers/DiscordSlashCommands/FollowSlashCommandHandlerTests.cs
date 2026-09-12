using Bogus;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Tests.E2E;
using FplBot.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.Handlers.DiscordSlashCommands;

[Collection("App")]
public class FollowSlashCommandHandlerTests(AppFixture fixture)
{
    private static readonly Faker Faker = new();

    private static int NewLeagueId() => Faker.Random.Int(100_000, 999_999);

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

        var response = await fixture.AskDiscord("follow", optionValue: leagueId.ToString());

        Assert.Contains($"Could not find a classic league of id '{leagueId}'", response.EmbedDescription());
    }

    [Fact]
    public async Task NoExistingSubscription_CreatesOneAndSubscribesToAll()
    {
        var leagueId = NewLeagueId();
        SetLeagueFound(leagueId, "Test League");

        var response = await fixture.AskDiscord("follow", optionValue: leagueId.ToString());

        Assert.Contains("Now following the 'Test League' FPL league", response.EmbedDescription());
        Assert.Contains("Auto-subbed to all events", response.EmbedDescription());
    }

    [Fact]
    public async Task ExistingSubscription_UpdatesLeague()
    {
        var sub = await fixture.SeedGuildSubscription();
        var leagueId = NewLeagueId();
        SetLeagueFound(leagueId, "Other League");

        var response = await fixture.AskDiscord("follow", optionValue: leagueId.ToString(), guildId: sub.GuildId, channelId: sub.ChannelId);

        Assert.Contains("Now following the 'Other League' FPL league", response.EmbedDescription());
    }
}
