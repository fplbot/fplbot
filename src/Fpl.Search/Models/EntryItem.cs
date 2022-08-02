using FplBot.VerifiedEntries.Data.Models;

namespace Fpl.Search.Models;

public class EntryItem
{
    public int Id { get; set; }
    public string RealName { get; set; }
    public string TeamName { get; set; }
    public VerifiedEntryType? VerifiedType { get; set; }
    public string Alias { get; set; }
    public string Description { get; set; }
    public string Country { get; set; }
    public int NumberOfPastSeasons { get; set; }
    public string Thumbprint { get; set; }
}
