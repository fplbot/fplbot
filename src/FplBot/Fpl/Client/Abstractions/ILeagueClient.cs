using Fpl.Client.Models;

namespace Fpl.Client.Abstractions;

public interface ILeagueClient
{
    Task<ClassicLeague> GetClassicLeague(int leagueId, int page = 1, bool tolerate404 = false);

    Task<HeadToHeadLeague> GetHeadToHeadLeague(int leagueId);
}
