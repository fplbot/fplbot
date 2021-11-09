namespace FplBot.VerifiedEntries.Data.Models;

public record VerifiedEntryStats(
    int CurrentGwTotalPoints,
    int LastGwTotalPoints,
    int OverallRank,
    int PointsThisGw,
    string ActiveChip,
    string Captain,
    string ViceCaptain,
    int Gameweek);