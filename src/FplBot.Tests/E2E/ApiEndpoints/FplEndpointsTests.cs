using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.DependencyInjection;
using FakeItEasy;

namespace FplBot.Tests.E2E.ApiEndpoints;

[Collection("App")]
public class FplEndpointsTests(AppFixture fixture)
{
    [Fact]
    public async Task GetEntry_ExistingEntry_ReturnsNameFromLiveFplApiNotSearchIndex()
    {
        const int entryId = 7744502;
        A.CallTo(() => fixture.Services.GetRequiredService<IEntryClient>().Get(entryId, true))
            .Returns(new BasicEntry { Id = entryId, TeamName = "Korsnes FC", PlayerFirstName = "John", PlayerLastName = "Korsnes" });

        var response = await fixture.Get($"/api/fpl/entries/{entryId}");

        response.EnsureSuccessStatusCode();
        var body = await AppFixture.ReadJson<EntryLookupResponse>(response);
        Assert.Equal(entryId, body.Id);
        Assert.Equal("Korsnes FC", body.TeamName);
        Assert.Equal("John Korsnes", body.RealName);
    }

    [Fact]
    public async Task GetEntry_UnknownEntry_ReturnsNotFound()
    {
        const int entryId = 999999991;
        A.CallTo(() => fixture.Services.GetRequiredService<IEntryClient>().Get(entryId, true))
            .Returns((BasicEntry?)null);

        var response = await fixture.Get($"/api/fpl/entries/{entryId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEntry_FplApiReturns200WithNotFoundDetail_ReturnsNotFound()
    {
        const int entryId = 999999992;
        A.CallTo(() => fixture.Services.GetRequiredService<IEntryClient>().Get(entryId, true))
            .Returns(new BasicEntry { Id = entryId, Detail = "Not found." });

        var response = await fixture.Get($"/api/fpl/entries/{entryId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private record EntryLookupResponse(int Id, string? TeamName, string? RealName);
}
