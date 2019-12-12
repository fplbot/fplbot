using System.Collections.Generic;
using System.Threading.Tasks;
using Fpl.Client.Models;

namespace Fpl.Client.Abstractions
{
    public interface IGameweekClient
    {
        Task<ICollection<Gameweek>> GetGameweeks();
    }
}
