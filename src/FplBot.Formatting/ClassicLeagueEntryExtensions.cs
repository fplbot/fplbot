using Fpl.Client.Models;

namespace FplBot.Formatting;

public static class ClassicLeagueEntryExtensions
{
    public static string GetEntryLink(this GenericEntry entry, int? gameweek)
    {
        return Formatter.GetEntryLink(entry.Entry, entry.EntryName, gameweek);
    }

    public static string GetEntryLink(this ClassicLeagueEntry entry, int? gameweek)
    {
        return Formatter.GetEntryLink(entry.Entry, entry.EntryName, gameweek);
    }


}