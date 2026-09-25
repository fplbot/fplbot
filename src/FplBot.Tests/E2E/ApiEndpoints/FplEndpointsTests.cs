using System.Net;
using System.Text.Json;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.E2E.ApiEndpoints;

[Collection("App")]
public class FplEndpointsTests(AppFixture fixture)
{
    [Fact]
    public async Task GetLeague_Found_ReturnsLeagueNameAndAdmin()
    {
        const int leagueId = 555;
        var leagueClient = fixture.Services.GetRequiredService<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(leagueId, A<int>._, A<bool>._)).Returns(new ClassicLeague
        {
            Properties = new ClassicLeagueProperties { Name = "Test League", AdminEntry = 1 },
            Standings = new ClassicLeagueStandings
            {
                Entries = [new ClassicLeagueEntry { Entry = 1, PlayerName = "Admin Player" }]
            }
        });

        var json = await fixture.GetJson<JsonElement>($"/api/fpl/leagues/{leagueId}");

        Assert.Equal("Test League", json.GetProperty("leagueName").GetString());
        Assert.Equal("Admin Player", json.GetProperty("leagueAdmin").GetString());
    }

    [Fact]
    public async Task GetLeague_NotFound_Returns404()
    {
        const int leagueId = 556;
        var leagueClient = fixture.Services.GetRequiredService<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(leagueId, A<int>._, A<bool>._)).Returns((ClassicLeague?)null);

        var response = await fixture.Get($"/api/fpl/leagues/{leagueId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLeagueDetails_Found_ReturnsGameweekAndStandings()
    {
        const int leagueId = 557;
        const int gameweekId = 9;

        var leagueClient = fixture.Services.GetRequiredService<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(leagueId, A<int>._, A<bool>._)).Returns(new ClassicLeague
        {
            Properties = new ClassicLeagueProperties { Name = "Details League", AdminEntry = 1 },
            Standings = new ClassicLeagueStandings
            {
                Entries =
                [
                    new ClassicLeagueEntry
                    {
                        Entry = 1, PlayerName = "Admin Player", EntryName = "Admin FC",
                        Rank = 1, LastRank = 1, Total = 100, EventTotal = 50
                    }
                ]
            }
        });

        var globalSettingsClient = fixture.Services.GetRequiredService<IGlobalSettingsClient>();
        A.CallTo(() => globalSettingsClient.GetGlobalSettings()).Returns(new GlobalSettings
        {
            Gameweeks = [new() { Id = gameweekId, Name = $"Gameweek {gameweekId}", IsCurrent = true }]
        });

        var transfersClient = fixture.Services.GetRequiredService<ITransfersClient>();
        A.CallTo(() => transfersClient.GetTransfers(1)).Returns((ICollection<Transfer>?)null);
        var entryHistoryClient = fixture.Services.GetRequiredService<IEntryHistoryClient>();
        A.CallTo(() => entryHistoryClient.GetHistory(1)).Returns(((int, EntryHistory)?)null);
        var entryClient = fixture.Services.GetRequiredService<IEntryClient>();
        A.CallTo(() => entryClient.GetPicks(1, gameweekId)).Returns((EntryPicks?)null);

        var json = await fixture.GetJson<JsonElement>($"/api/fpl/leagues/{leagueId}/details");

        Assert.Equal("Details League", json.GetProperty("leagueName").GetString());
        Assert.Equal(gameweekId, json.GetProperty("gameweek").GetInt32());
        Assert.Equal(1, json.GetProperty("standings").GetArrayLength());
    }

    [Fact]
    public async Task GetLeagueDetails_NoStandings_Returns404()
    {
        const int leagueId = 558;
        var leagueClient = fixture.Services.GetRequiredService<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(leagueId, A<int>._, A<bool>._)).Returns(new ClassicLeague { Standings = null });

        var response = await fixture.Get($"/api/fpl/leagues/{leagueId}/details");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
