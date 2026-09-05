using Fpl.Client.Models;

namespace Fpl.Client.Abstractions;

public interface IEntryHistoryClient
{
    Task<(int teamId, EntryHistory entryHistory)?> GetHistory(int teamId, bool tolerate404 = false);
}
