using Fpl.Client.Models;
using FplBot.Slack.Helpers;
using FplBot.Slack.Helpers.Formatting;

namespace FplBot.Slack.Extensions
{
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
}
