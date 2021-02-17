using System.Collections.Generic;
using System.Threading.Tasks;
using Fpl.Client.Models;

namespace Fpl.Client.Abstractions
{
    public interface ILiveClient
    {
        Task<ICollection<LiveItem>> GetLiveItems(int gameweek);
    }
}