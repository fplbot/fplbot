using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot.Helpers;

namespace Slackbot.Net.Extensions.FplBot.Extensions
{
    public static class ClassicLeagueEntryExtensions
    {
        public static string GetEntryLink(this GenericEntry entry, int? gameweek)
        {
            return $"<https://fantasy.premierleague.com/entry/{entry.Entry}/event/{gameweek}|{entry.EntryName}>";
        }
        
        public static string GetEntryLink(this ClassicLeagueEntry entry, int? gameweek)
        {
            return $"<https://fantasy.premierleague.com/entry/{entry.Entry}/event/{gameweek}|{entry.EntryName}>";
        }
    }
}
