namespace FplBot.VerifiedEntries.Data.Models;

public record VerifiedEntry(
    int EntryId,
    string FullName,
    string EntryTeamName,
    VerifiedEntryType VerifiedEntryType,
    string Alias = null,
    string Description = null,
    VerifiedEntryStats EntryStats = null
);
