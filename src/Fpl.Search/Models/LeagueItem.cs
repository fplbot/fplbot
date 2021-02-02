namespace Fpl.Search.Models
{
    public class LeagueItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? AdminEntry { get; set; }
        public string AdminName { get; set; }
        public string AdminTeamName { get; set; }
        public string AdminCountry { get; set; }
    }
}