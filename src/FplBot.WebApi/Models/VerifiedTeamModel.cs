namespace FplBot.WebApi.Models
{
    public class VerifiedTeamModel
    {
        public int EntryId { get; set; }
        public string TeamName { get; set; }
        public string RealName { get; set; }
        public string PlName { get; set; }
        public string PlaysForTeam { get; set; }
        public string ImageUrl { get; set; }
        public int? PointsThisGw { get; set; }
        public int? TotalPoints { get; set; }
        public int? OverallRank { get; set; }
        public string Captain { get; set; }
        public string ViceCaptain { get; set; }
        public string ChipUsed { get; set; }
        public int SelfOwnershipWeekCount { get; set; }
        public int SelfOwnershipTotalPoints { get; set; }
    }
}