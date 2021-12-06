using FplBot.VerifiedEntries.Data.Models;

namespace FplBot.VerifiedEntries.Data.Abstractions;

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
