namespace FplBot.VerifiedEntries.Data.Models
{
    public record VerifiedPLEntry(
        int EntryId,
        long TeamId,
        long TeamCode,
        string TeamName,
        int PlayerId,
        int PlayerCode,
        string PlayerWebName,
        string PlayerFullName,
        SelfOwnershipStats SelfOwnershipStats = null
    );

    public record SelfOwnershipStats(int WeekCount, int TotalPoints, int Gameweek);

}
