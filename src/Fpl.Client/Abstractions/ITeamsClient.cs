using System.Collections.Generic;
using System.Threading.Tasks;
using Fpl.Client.Models;

namespace Fpl.Client.Abstractions
{
    public interface ITeamsClient
    {
        Task<ICollection<Team>> GetAllTeams();
        Task<Team> GetTeam(int teamId);
    }
}