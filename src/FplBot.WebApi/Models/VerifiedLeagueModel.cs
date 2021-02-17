using System.Collections.Generic;

namespace FplBot.WebApi.Models
{
    public class VerifiedLeagueModel
    {
        public int Gameweek { get; set; }
        public IEnumerable<VerifiedLeagueItem> Entries { get; set; }
    }

    public class VerifiedLeagueItem
    {
        public int EntryId { get; set; }
        public string Slug { get; set; }
        public string TeamName { get; set; }
        public string RealName { get; set; }
        public int? PLPlayerId { get; set; }
        public string PLName { get; set; }
        public string PlaysForTeam { get; set; }
        public string ShirtImageUrl { get; set; }
        public string ImageUrl { get; set; }
        public int? PointsThisGw { get; set; }
        public int? TotalPoints { get; set; }
        public int? TotalPointsLastGw { get; set; }
        public int? OverallRank { get; set; }
        public string Captain { get; set; }
        public string ViceCaptain { get; set; }
        public string ChipUsed { get; set; }
        public int Movement { get; set; }
    }
}