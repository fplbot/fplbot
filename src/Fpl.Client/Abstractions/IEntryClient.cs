using Fpl.Client.Models;

namespace Fpl.Client.Abstractions;

public interface IEntryClient
{
    Task<BasicEntry> Get(int teamId, bool tolerate404 = false);

    Task<EntryPicks> GetPicks(int teamId, int gameweek, bool tolerate404 = false);
}