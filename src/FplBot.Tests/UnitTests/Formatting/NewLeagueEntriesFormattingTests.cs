using Fpl.Client.Models;
using FplBot.Formatting;

namespace FplBot.Tests.UnitTests.Formatting;

public class NewLeagueEntriesFormattingTests
{
    [Fact]
    public void SingleEntry_UsesSingularHeader()
    {
        var formatted = Formatter.FormatNewLeagueEntries("YOLO league", [Entrant("John", "Korsnes", "Takk for meg")]);

        Assert.Contains("New entry in YOLO league!", formatted);
        Assert.Contains("John Korsnes (Takk for meg)", formatted);
        Assert.DoesNotContain("a bunch more", formatted);
    }

    [Fact]
    public void SeveralEntries_ListsAllWithPluralHeader()
    {
        var formatted = Formatter.FormatNewLeagueEntries("YOLO league",
            [Entrant("John", "Korsnes", "Takk for meg"), Entrant("Ada", "Lovelace", "Analytical FC")]);

        Assert.Contains("New entries in YOLO league!", formatted);
        Assert.Contains("John Korsnes (Takk for meg)", formatted);
        Assert.Contains("Ada Lovelace (Analytical FC)", formatted);
        Assert.DoesNotContain("a bunch more", formatted);
    }

    [Fact]
    public void MoreThanFive_ListsFiveThenSaysBunchMore()
    {
        var entrants = Enumerable.Range(1, 9).Select(i => Entrant("Player", i.ToString(), $"Team {i}")).ToList();

        var formatted = Formatter.FormatNewLeagueEntries("YOLO league", entrants);

        Assert.Equal(Formatter.MaxListedNewLeagueEntries, formatted.Split("▪️").Length - 1);
        Assert.Contains("Player 5 (Team 5)", formatted);
        Assert.DoesNotContain("Player 6", formatted);
        Assert.Contains("…along with a bunch more", formatted);
    }

    [Fact]
    public void WhenMorePagesExist_SaysBunchMoreEvenWithFewListed()
    {
        var formatted = Formatter.FormatNewLeagueEntries("YOLO league", [Entrant("John", "Korsnes", "Takk for meg")], hasMore: true);

        Assert.Contains("New entries in YOLO league!", formatted);
        Assert.Contains("…along with a bunch more", formatted);
    }

    [Fact]
    public void WithoutHeader_ListsEntriesOnly()
    {
        var formatted = Formatter.FormatNewLeagueEntries("YOLO league",
            [Entrant("John", "Korsnes", "Takk for meg")], includeHeader: false);

        Assert.DoesNotContain("YOLO league", formatted);
        Assert.Equal("▪️ John Korsnes (Takk for meg)", formatted.Trim());
    }

    [Fact]
    public void WithoutHeader_StillSaysBunchMore()
    {
        var formatted = Formatter.FormatNewLeagueEntries("YOLO league",
            [Entrant("John", "Korsnes", "Takk for meg")], hasMore: true, includeHeader: false);

        Assert.DoesNotContain("YOLO league", formatted);
        Assert.Contains("…along with a bunch more", formatted);
    }

    [Fact]
    public void WhenPlayerNameMissing_FallsBackToEntryName()
    {
        var formatted = Formatter.FormatNewLeagueEntries("YOLO league", [Entrant(null, null, "Takk for meg")]);

        Assert.Contains("▪️ Takk for meg", formatted);
        Assert.DoesNotContain("()", formatted);
    }

    [Fact]
    public void WhenEntryNameMissing_FallsBackToPlayerName()
    {
        var formatted = Formatter.FormatNewLeagueEntries("YOLO league", [Entrant("John", "Korsnes", null)]);

        Assert.Contains("▪️ John Korsnes", formatted);
        Assert.DoesNotContain("()", formatted);
    }

    private static NewLeagueEntry Entrant(string? firstName, string? lastName, string? entryName) =>
        new()
        {
            Entry = 1,
            PlayerFirstName = firstName,
            PlayerLastName = lastName,
            EntryName = entryName,
            JoinedAt = new DateTime(2026, 9, 15, 20, 2, 5, DateTimeKind.Utc)
        };
}
