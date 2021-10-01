using Fpl.Client.Models;
using System.Collections.Generic;
using System.Linq;

namespace FplBot.Core.Extensions
{
    public static class TeamsExtensions
    {
        public static Team Get(this ICollection<Team> teams, int? teamId)
        {
            return teamId.HasValue ? teams.SingleOrDefault(p => p.Id == teamId) : null;
        }
    }
}