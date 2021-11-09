using Fpl.Client.Models;

namespace Fpl.Client.Abstractions;

public interface IEntryHistoryClient
{
    Task<EntryHistory> GetHistory(int teamId);
}