using System.Collections.Generic;
using System.Threading.Tasks;
using Fpl.Data.Models;
using Fpl.Data.Repositories;

namespace Fpl.Data.Abstractions
{
    public interface IVerifiedPLEntriesRepository
    {
        Task Insert(VerifiedPLEntry entry);
        Task<IEnumerable<VerifiedPLEntry>> GetAllVerifiedPLEntries();
        Task<VerifiedPLEntry> GetVerifiedPLEntry(int entryId);

        Task Delete(int entryId);
        Task DeleteAll();
        Task UpdateStats(int entryId, SelfOwnershipStats selfOwnershipStats);
    }
}
