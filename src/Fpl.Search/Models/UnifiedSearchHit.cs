using FplBot.Data.Models;

namespace Fpl.Search.Models
{
    public class UnifiedSearchHit : IEntryItem, ILeagueItem
    {
        public SearchHitType Type { get; set; }
        public int Id { get; set; }
        public string RealName { get; set; }
        public string TeamName { get; set; }
        public VerifiedEntryType? VerifiedType { get; set; }
        public string Alias { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
        public int? AdminEntry { get; set; }
        public string AdminName { get; set; }
        public string AdminTeamName { get; set; }
        public string AdminCountry { get; set; }
    }

    public enum SearchHitType
    {
        Entry,
        League
    }
}
