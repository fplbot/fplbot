using Fpl.Client.Models;

namespace Slackbot.Net.Extensions.FplBot.Extensions
{
    public static class ClassicLeagueEntryExtensions
    {
        public static string GetEntryLink(this ClassicLeagueEntry entry, int? gameweek)
        {
            return $"<https://fantasy.premierleague.com/entry/{entry.Entry}/event/{gameweek}|{entry.EntryName}>";
        }
    }
}
