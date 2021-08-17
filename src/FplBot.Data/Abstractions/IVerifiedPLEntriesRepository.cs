using System.Collections.Generic;
using System.Threading.Tasks;
using FplBot.Data.Models;

namespace FplBot.Data.Abstractions
{
    public interface IVerifiedPLEntriesRepository
    {
        Task Insert(VerifiedPLEntry entry);
        Task<IEnumerable<VerifiedPLEntry>> GetAllVerifiedPLEntries();
        Task<VerifiedPLEntry> GetVerifiedPLEntry(int entryId);

        Task Delete(int entryId);
        Task DeleteAll();
        Task DeleteAllOfThese(int[] entryIds);
        Task UpdateStats(int entryId, SelfOwnershipStats selfOwnershipStats);
    }
}
