namespace Fpl.Search.Models
{
    public class LeagueItem : IIndexableItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? AdminEntry { get; set; }
    }
}