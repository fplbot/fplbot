using System.Threading.Tasks;
using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot.Helpers;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    public interface IEntryForGameweek
    {
        Task<GameweekEntry> GetEntryForGameweek(ClassicLeagueEntry entry, int gameweek);
        Task<GameweekEntry> GetEntryForGameweek(GenericEntry entry, int gameweek);
    }
}