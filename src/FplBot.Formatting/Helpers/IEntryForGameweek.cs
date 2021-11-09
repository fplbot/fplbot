using System.Threading.Tasks;
using Fpl.Client.Models;

namespace FplBot.Formatting.Helpers;

public interface IEntryForGameweek
{
    Task<GameweekEntry> GetEntryForGameweek(ClassicLeagueEntry entry, int gameweek);
    Task<GameweekEntry> GetEntryForGameweek(GenericEntry entry, int gameweek);
}