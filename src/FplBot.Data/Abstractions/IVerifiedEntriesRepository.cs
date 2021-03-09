using System.Collections.Generic;
using System.Threading.Tasks;
using FplBot.Data.Models;

namespace FplBot.Data.Abstractions
{
    public interface IVerifiedEntriesRepository
    {
        Task Insert(VerifiedEntry entry);
        Task<IEnumerable<VerifiedEntry>> GetAllVerifiedEntries();
        Task<VerifiedEntry> GetVerifiedEntry(int entryId);
        Task Delete(int entryId);
        Task DeleteAll();
        Task UpdateAllStats(int entryId, VerifiedEntryStats verifiedEntryStats);
        Task UpdateLiveStats(int entryId, VerifiedEntryPointsUpdate newStats);
    }
}
