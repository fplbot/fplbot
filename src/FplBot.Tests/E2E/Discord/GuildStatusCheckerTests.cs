using System.Net;
using System.Text;
using Discord.Net.HttpClients;
using FakeItEasy;
using FplBot.Data;
using FplBot.WebApi.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace FplBot.Tests.E2E.Discord;

[Collection("App")]
public class GuildStatusCheckerTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.FlushRedisAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GuildNoLongerReachable_DeletesGuildAndItsSubscriptions()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        var sut = BuildChecker(HttpStatusCode.NotFound, installedGuild.ExternalId);

        await sut.Process(CancellationToken.None);

        Assert.Null(await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.ExternalId));
    }

    [Fact]
    public async Task GuildNoLongerReachable_RemovesItsStoredMemberCount()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        await fixture.GuildMemberCountRepo.SetApproximateMemberCount(installedGuild.ExternalId, 42);
        var sut = BuildChecker(HttpStatusCode.NotFound, installedGuild.ExternalId);

        await sut.Process(CancellationToken.None);

        Assert.DoesNotContain(installedGuild.ExternalId, (await fixture.GuildMemberCountRepo.GetAll()).Keys);
    }

    [Fact]
    public async Task GuildStillReachable_KeepsGuildAndSubscriptions()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        var sut = BuildChecker(HttpStatusCode.OK, installedGuild.ExternalId);

        await sut.Process(CancellationToken.None);

        var remaining = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.ExternalId);
        Assert.NotNull(remaining);
        Assert.NotEmpty(remaining.ChannelSubscriptions);
    }

    [Fact]
    public async Task GuildStillReachable_PersistsItsApproximateMemberCount()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        var sut = BuildChecker(HttpStatusCode.OK, installedGuild.ExternalId, approximateMemberCount: 123);

        await sut.Process(CancellationToken.None);

        var counts = await fixture.GuildMemberCountRepo.GetAll();
        Assert.Equal(123, counts[installedGuild.ExternalId]);
    }

    private GuildStatusChecker BuildChecker(HttpStatusCode statusCode, string guildId, int approximateMemberCount = 0)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(statusCode, guildId, approximateMemberCount)) { BaseAddress = new Uri("https://discord.example/") };
        var discordClient = new DiscordClient(httpClient,
            Options.Create(new DiscordClientOptions { DiscordApplicationId = "test", DiscordAppToken = "test" }),
            NullLogger<DiscordClient>.Instance);
        return new GuildStatusChecker(fixture.GuildRepo, fixture.GuildMemberCountRepo, discordClient, fixture.Services.GetRequiredService<IServiceScopeFactory>(), NullLogger<GuildStatusChecker>.Instance);
    }

    private class StubHttpMessageHandler(HttpStatusCode statusCode, string guildId, int approximateMemberCount) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode);
            if (statusCode == HttpStatusCode.OK)
            {
                response.Content = new StringContent(
                    $$"""{"id":"{{guildId}}","approximate_member_count":{{approximateMemberCount}}}""", Encoding.UTF8, "application/json");
            }

            return Task.FromResult(response);
        }
    }
}
