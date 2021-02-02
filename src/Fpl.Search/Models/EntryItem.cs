namespace Fpl.Search.Models
{
    public class EntryItem
    {
        public int Id { get; set; }
        public int Entry { get; set; }
        public string RealName { get; set; }
        public string TeamName { get; set; }
        public VerifiedEntryType? VerifiedType { get; set; }
    }
}
