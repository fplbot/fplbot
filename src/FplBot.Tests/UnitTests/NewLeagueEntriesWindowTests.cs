using Fpl.Client.Models;
using FplBot.EventHandlers;

namespace FplBot.Tests.UnitTests;

public class NewLeagueEntriesWindowTests
{
    [Fact]
    public void WindowStartsAtThePreviousGameweeksDeadline()
    {
        Assert.Equal(new DateTime(2026, 9, 12, 12, 30, 0, DateTimeKind.Utc),
            NewLeagueEntriesLookup.PreviousDeadline(Settings(), 5));
    }

    [Fact]
    public void WhenThePreviousGameweekIsUnknown_ThereIsNoWindow()
    {
        Assert.Null(NewLeagueEntriesLookup.PreviousDeadline(Settings(), 1));
        Assert.Null(NewLeagueEntriesLookup.PreviousDeadline(null, 5));
    }

    [Fact]
    public void DeadlineWithoutAKind_IsReadAsUtc_NotLocal()
    {
        var settings = new GlobalSettings { Gameweeks = [new Gameweek { Id = 4, Deadline = new DateTime(2026, 9, 12, 12, 30, 0, DateTimeKind.Unspecified) }] };

        var deadline = NewLeagueEntriesLookup.PreviousDeadline(settings, 5);

        Assert.Equal(DateTimeKind.Utc, deadline!.Value.Kind);
        Assert.Equal(new DateTime(2026, 9, 12, 12, 30, 0, DateTimeKind.Utc), deadline.Value);
    }

    private static GlobalSettings Settings() => new()
    {
        Gameweeks =
        [
            new Gameweek { Id = 3, Deadline = new DateTime(2026, 9, 4, 17, 30, 0, DateTimeKind.Utc) },
            new Gameweek { Id = 4, Deadline = new DateTime(2026, 9, 12, 12, 30, 0, DateTimeKind.Utc) },
            new Gameweek { Id = 5, Deadline = new DateTime(2026, 9, 18, 17, 30, 0, DateTimeKind.Utc) }
        ]
    };
}
