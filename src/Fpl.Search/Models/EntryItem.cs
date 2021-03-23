using FplBot.Data.Models;

namespace Fpl.Search.Models
{
    public class EntryItem : IEntryItem
    {
        public int Id { get; set; }
        public string RealName { get; set; }
        public string TeamName { get; set; }
        public VerifiedEntryType? VerifiedType { get; set; }
        public string Alias { get; set; }
        public string Description { get; set; }
    }

    public interface IEntryItem
    {
        int Id { get; set; }
        string RealName { get; set; }
        string TeamName { get; set; }
        VerifiedEntryType? VerifiedType { get; set; }
        string Alias { get; set; }
        string Description { get; set; }
    }
}
