using Fpl.Client.Models;

namespace Fpl.Client.Abstractions;

public interface ILiveClient
{
    Task<ICollection<LiveItem>> GetLiveItems(int gameweek, bool isOngoingGameweek = false);
}