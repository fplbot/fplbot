using System.Threading.Tasks;
using Fpl.Client.Models;
using FplBot.Formatting;
using FplBot.Slack.Helpers;

namespace FplBot.Slack.Abstractions
{
    public interface IEntryForGameweek
    {
        Task<GameweekEntry> GetEntryForGameweek(ClassicLeagueEntry entry, int gameweek);
        Task<GameweekEntry> GetEntryForGameweek(GenericEntry entry, int gameweek);
    }
}
