using FplBot.VerifiedEntries.Data.Models;

namespace FplBot.WebApi.Controllers;

public static class ApiModelBuilder
{
    public static PLVirtualLeagueEntry BuildPLVirtualLeagueEntry(VerifiedPLEntry plEntry, VerifiedEntry fplEntry)
    {
        return new(
            EntryId : fplEntry.EntryId,
            Slug: plEntry.PlayerFullName.ToLower().Replace(" ", "-"),
            TeamName : fplEntry.EntryTeamName,
            RealName : fplEntry.FullName,
            Alias: fplEntry.Alias,
            Description: fplEntry.Description,
            PointsThisGw: fplEntry.EntryStats?.PointsThisGw,
            TotalPoints: fplEntry.EntryStats?.CurrentGwTotalPoints,
            OverallRank: fplEntry.EntryStats?.OverallRank,
            Movement:0,
            Captain: fplEntry.EntryStats?.Captain,
            ViceCaptain: fplEntry.EntryStats?.ViceCaptain,
            ChipUsed: fplEntry.EntryStats?.ActiveChip,
            Gameweek: fplEntry.EntryStats?.Gameweek,
            PLPlayerId: plEntry.PlayerId,
            PLName : plEntry.PlayerFullName,
            PlaysForTeam : plEntry.TeamName,
            ShirtImageUrl : $"https://fantasy.premierleague.com/dist/img/shirts/standard/shirt_{plEntry.TeamCode}-66.png", // move url-gen to client ?
            ImageUrl : $"https://resources.premierleague.com/premierleague/photos/players/110x140/p{plEntry.PlayerCode}.png", // move url-gen to client ?
            SelfOwnershipWeekCount : plEntry.SelfOwnershipStats.WeekCount,
            SelfOwnershipTotalPoints : plEntry.SelfOwnershipStats.TotalPoints
        );
    }

    public static RegularVirtualLeagueEntry BuildVirtualLeagueEntry(VerifiedEntry fplEntry)
    {
        return new (
            EntryId : fplEntry.EntryId,
            VerifiedType: fplEntry.VerifiedEntryType.ToString(),
            TeamName : fplEntry.EntryTeamName,
            RealName : fplEntry.FullName,
            Alias: fplEntry.Alias,
            Description: fplEntry.Description,
            PointsThisGw: fplEntry.EntryStats?.PointsThisGw,
            TotalPoints: fplEntry.EntryStats?.CurrentGwTotalPoints,
            OverallRank: fplEntry.EntryStats?.OverallRank,
            Movement:0,
            Captain: fplEntry.EntryStats?.Captain,
            ViceCaptain: fplEntry.EntryStats?.ViceCaptain,
            ChipUsed: fplEntry.EntryStats?.ActiveChip,
            Gameweek: fplEntry.EntryStats?.Gameweek
        );
    }

    public static RegularEntry BuildRegularEntry(VerifiedEntry fplEntry)
    {
        return new (
            EntryId : fplEntry.EntryId,
            VerifiedType: fplEntry.VerifiedEntryType.ToString(),
            TeamName : fplEntry.EntryTeamName,
            RealName : fplEntry.FullName,
            Alias: fplEntry.Alias,
            Description: fplEntry.Description,
            PointsThisGw: fplEntry.EntryStats?.PointsThisGw,
            TotalPoints: fplEntry.EntryStats?.CurrentGwTotalPoints,
            OverallRank: fplEntry.EntryStats?.OverallRank,
            Captain: fplEntry.EntryStats?.Captain,
            ViceCaptain: fplEntry.EntryStats?.ViceCaptain,
            ChipUsed: fplEntry.EntryStats?.ActiveChip,
            Gameweek: fplEntry.EntryStats?.Gameweek
        );
    }

    public static PLEntry BuildPLEntry(VerifiedEntry fplEntry, VerifiedPLEntry plEntry)
    {
        return new(
            EntryId : fplEntry.EntryId,
            Slug: plEntry.PlayerFullName.ToLower().Replace(" ", "-"),
            TeamName : fplEntry.EntryTeamName,
            RealName : fplEntry.FullName,
            Alias: fplEntry.Alias,
            Description: fplEntry.Description,
            PointsThisGw: fplEntry.EntryStats?.PointsThisGw,
            TotalPoints: fplEntry.EntryStats?.CurrentGwTotalPoints,
            OverallRank: fplEntry.EntryStats?.OverallRank,
            Captain: fplEntry.EntryStats?.Captain,
            ViceCaptain: fplEntry.EntryStats?.ViceCaptain,
            ChipUsed: fplEntry.EntryStats?.ActiveChip,
            Gameweek: fplEntry.EntryStats?.Gameweek,
            PLPlayerId: plEntry.PlayerId,
            PLName : plEntry.PlayerFullName,
            PlaysForTeam : plEntry.TeamName,
            ShirtImageUrl : $"https://fantasy.premierleague.com/dist/img/shirts/standard/shirt_{plEntry.TeamCode}-66.png", // move url-gen to client ?
            ImageUrl : $"https://resources.premierleague.com/premierleague/photos/players/110x140/p{plEntry.PlayerCode}.png", // move url-gen to client ?
            SelfOwnershipWeekCount : plEntry.SelfOwnershipStats.WeekCount,
            SelfOwnershipTotalPoints : plEntry.SelfOwnershipStats.TotalPoints
        );
    }
}
public record PLVirtualLeagueEntry(
    int EntryId,
    string Slug,
    string TeamName,
    string RealName,
    string Alias,
    string Description,
    int? PLPlayerId,
    string PLName,
    string PlaysForTeam,
    string ShirtImageUrl,
    string ImageUrl,
    int? PointsThisGw,
    int? TotalPoints,
    int? OverallRank,
    int Movement,
    string Captain,
    string ViceCaptain,
    string ChipUsed,
    int SelfOwnershipWeekCount,
    int SelfOwnershipTotalPoints,
    int? Gameweek
);

public record PLEntry(
    int EntryId,
    string Slug,
    string TeamName,
    string RealName,
    string Alias,
    string Description,
    int? PLPlayerId,
    string PLName,
    string PlaysForTeam,
    string ShirtImageUrl,
    string ImageUrl,
    int? PointsThisGw,
    int? TotalPoints,
    int? OverallRank,
    string Captain,
    string ViceCaptain,
    string ChipUsed,
    int SelfOwnershipWeekCount,
    int SelfOwnershipTotalPoints,
    int? Gameweek
);


public record RegularVirtualLeagueEntry(
    int EntryId,
    string VerifiedType,
    string TeamName,
    string RealName,
    string Alias,
    string Description,
    int? PointsThisGw,
    int? TotalPoints,
    int? OverallRank,
    int Movement,
    string Captain,
    string ViceCaptain,
    string ChipUsed,
    int? Gameweek
);

public record RegularEntry(
    int EntryId,
    string VerifiedType,
    string TeamName,
    string RealName,
    string Alias,
    string Description,
    int? PointsThisGw,
    int? TotalPoints,
    int? OverallRank,
    string Captain,
    string ViceCaptain,
    string ChipUsed,
    int? Gameweek
);
