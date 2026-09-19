using System.Net;
using System.Text;
using Discord.Net.HttpClients;
using FakeItEasy;
using FplBot.Data;
using FplBot.WebApi.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        var sut = BuildChecker(HttpStatusCode.NotFound, installedGuild.Id);

        await sut.Process(CancellationToken.None);

        Assert.Null(await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.Id));
    }

    [Fact]
    public async Task GuildStillReachable_KeepsGuildAndSubscriptions()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Standings]);
        var sut = BuildChecker(HttpStatusCode.OK, installedGuild.Id);

        await sut.Process(CancellationToken.None);

        var remaining = await fixture.GuildRepo.FindInstallationByTeamId(installedGuild.Id);
        Assert.NotNull(remaining);
        Assert.NotEmpty(remaining.ChannelSubscriptions);
    }

    private GuildStatusChecker BuildChecker(HttpStatusCode statusCode, string guildId)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(statusCode, guildId)) { BaseAddress = new Uri("https://discord.example/") };
        var discordClient = new DiscordClient(httpClient,
            Options.Create(new DiscordClientOptions { DiscordApplicationId = "test", DiscordAppToken = "test" }),
            A.Fake<ILogger<DiscordClient>>());
        return new GuildStatusChecker(fixture.GuildRepo, discordClient, fixture.Services.GetRequiredService<IServiceScopeFactory>(), A.Fake<ILogger<GuildStatusChecker>>());
    }

    private class StubHttpMessageHandler(HttpStatusCode statusCode, string guildId) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode);
            if (statusCode == HttpStatusCode.OK)
            {
                response.Content = new StringContent($$"""{"id":"{{guildId}}"}""", Encoding.UTF8, "application/json");
            }

            return Task.FromResult(response);
        }
    }
}
