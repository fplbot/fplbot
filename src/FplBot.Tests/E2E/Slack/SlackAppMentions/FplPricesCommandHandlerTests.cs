using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.E2E.Slack.SlackAppMentions;

[Collection("App")]
public class FplPricesCommandHandlerTests(AppFixture fixture) : IAsyncLifetime
{
    private GlobalSettings? _originalGlobalSettings;

    public async ValueTask InitializeAsync()
    {
        _originalGlobalSettings = await fixture.Services.GetRequiredService<IGlobalSettingsClient>().GetGlobalSettings();
    }

    public ValueTask DisposeAsync()
    {
        A.CallTo(() => fixture.Services.GetRequiredService<IGlobalSettingsClient>().GetGlobalSettings()).Returns(_originalGlobalSettings);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task GetPrices_RelevantChangedPlayer_IsIncluded()
    {
        var globalSettingsClient = fixture.Services.GetRequiredService<IGlobalSettingsClient>();
        A.CallTo(() => globalSettingsClient.GetGlobalSettings()).Returns(new GlobalSettings
        {
            Teams = [new Team { Id = 1, Code = 100, ShortName = "ARS" }],
            Players =
            [
                new Player { Id = 1, WebName = "Risen Star", OwnershipPercentage = 20, CostChangeEvent = 1, NowCost = 81, TeamCode = 100 },
                new Player { Id = 2, WebName = "Unchanged Player", OwnershipPercentage = 20, CostChangeEvent = 0, NowCost = 80, TeamCode = 100 },
                new Player { Id = 3, WebName = "Irrelevant Changed Player", OwnershipPercentage = 1, CostChangeEvent = 1, NowCost = 45, TeamCode = 100 }
            ]
        });

        await fixture.AskSlackbot("@fplbot pricechanges");
        var response = await fixture.SlackCapture.WaitForMessageAsync();

        Assert.Contains("Risen Star", response.AllText());
        Assert.DoesNotContain("Unchanged Player", response.AllText());
        Assert.DoesNotContain("Irrelevant Changed Player", response.AllText());
    }
}
