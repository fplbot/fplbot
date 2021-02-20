using System.Threading.Tasks;
using Fpl.Client.Models;

namespace Fpl.Client.Abstractions
{
    public interface IEntryClient
    {
        Task<BasicEntry> Get(int teamId);

        Task<EntryPicks> GetPicks(int teamId, int gameweek, bool tolerate404 = false);
    }
}