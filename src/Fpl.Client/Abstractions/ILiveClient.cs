using Fpl.Client.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fpl.Client.Abstractions
{
    public interface ILiveClient
    {
        Task<ICollection<LiveItem>> GetLiveItems(int gameweek, bool isOngoingGameweek = false);
    }
}