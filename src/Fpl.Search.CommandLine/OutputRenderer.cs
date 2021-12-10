using Fpl.Search.Models;
using Spectre.Console;

namespace Fpl.SearchConsole;

internal static class OutputRenderer
{
    public static void RenderEntriesTable(SearchResult<EntryItem> entryResult)
    {
        var entryTable = new Table();
        entryTable.Border(TableBorder.Rounded);
        entryTable.Collapse();
        entryTable.AddColumn(new TableColumn("Team").Centered());
        entryTable.AddColumn(new TableColumn("Name").Centered());
        entryTable.AddColumn(new TableColumn("Id").Centered());
        entryTable.AddColumn(new TableColumn("Verified type").Centered());

        foreach (var entry in entryResult.ExposedHits)
        {
            entryTable.AddRow(entry.TeamName, entry.RealName, entry.Id.ToString(),
                entry.VerifiedType.ToString() ?? string.Empty);
        }

        var rule = new Rule($"[green]Top {entryResult.Count} hits[/]");
        rule.LeftAligned();
        AnsiConsole.Render(rule);
        AnsiConsole.Render(entryTable);
        AnsiConsole.Render(rule);
    }

    public static void RenderLeaguesTable(SearchResult<LeagueItem> leagueResult)
    {
        var leagueTable = new Table();
        leagueTable.Border(TableBorder.Rounded);
        leagueTable.Collapse();
        leagueTable.AddColumn(new TableColumn("Name").Centered());
        leagueTable.AddColumn(new TableColumn("Id").Centered());
        leagueTable.AddColumn(new TableColumn("Admin entry").Centered());

        foreach (var entry in leagueResult.ExposedHits)
        {
            leagueTable.AddRow(entry.Name, entry.Id.ToString(), entry.AdminEntry.ToString());
        }

        var rule = new Rule($"[green]Top {leagueResult.Count} hits[/]");
        rule.LeftAligned();
        AnsiConsole.Render(rule);
        AnsiConsole.Render(leagueTable);
        AnsiConsole.Render(rule);
    }
}
