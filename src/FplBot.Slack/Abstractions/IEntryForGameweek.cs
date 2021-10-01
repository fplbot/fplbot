using System.Threading.Tasks;
using Fpl.Client.Models;
using FplBot.Core.Helpers;

namespace FplBot.Core.Abstractions
{
    public interface IEntryForGameweek
    {
        Task<GameweekEntry> GetEntryForGameweek(ClassicLeagueEntry entry, int gameweek);
        Task<GameweekEntry> GetEntryForGameweek(GenericEntry entry, int gameweek);
    }
}