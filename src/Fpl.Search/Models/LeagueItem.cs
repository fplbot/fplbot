namespace Fpl.Search.Models
{
    public class LeagueItem : ILeagueItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? AdminEntry { get; set; }
        public string AdminName { get; set; }
        public string AdminTeamName { get; set; }
        public string AdminCountry { get; set; }
    }

    public interface ILeagueItem
    {
        int Id { get; set; }
        string Name { get; set; }
        int? AdminEntry { get; set; }
        string AdminName { get; set; }
        string AdminTeamName { get; set; }
        string AdminCountry { get; set; }
    }
}
