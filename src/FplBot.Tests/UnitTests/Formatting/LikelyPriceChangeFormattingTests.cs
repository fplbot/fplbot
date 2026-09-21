using FplBot.Formatting;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.UnitTests.Formatting;

public class LikelyPriceChangeFormattingTests
{
    [Fact]
    public void FormatLikelyPriceChanges_WhenEmpty_ReturnsNoPlayersMessage()
    {
        var formatted = Formatter.FormatLikelyPriceChanges([]);

        Assert.Equal("No players likely to change price.", formatted);
    }

    [Fact]
    public void FormatLikelyPriceChanges_GroupsRisersAndFallersSeparately()
    {
        var players = new List<PlayerLikelyPriceChange>
        {
            new(1, "Haaland", 145, 11, "MCI", "123.2", 5),
            new(2, "Saka", 90, 1, "ARS", "-110.0", -5)
        };

        var formatted = Formatter.FormatLikelyPriceChanges(players);

        Assert.Contains("Likely to rise 📈", formatted);
        Assert.Contains("Haaland", formatted);
        Assert.Contains("Likely to fall 📉", formatted);
        Assert.Contains("Saka", formatted);
        Assert.True(formatted.IndexOf("Haaland", StringComparison.Ordinal) < formatted.IndexOf("Saka", StringComparison.Ordinal));
    }

    [Fact]
    public void FormatLikelyPriceChanges_WithoutMarkdown_OmitsAsterisks()
    {
        var players = new List<PlayerLikelyPriceChange> { new(1, "Haaland", 145, 11, "MCI", "123.2", 5) };

        var formatted = Formatter.FormatLikelyPriceChanges(players, markdown: false);

        Assert.DoesNotContain("*", formatted);
    }
}
