using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;

namespace FplBot.VerifiedEntries.Extensions
{
    public static class TeamsExtensions
    {
        public static Team Get(this ICollection<Team> teams, int? teamId)
        {
            return teamId.HasValue ? teams.SingleOrDefault(p => p.Id == teamId) : null;
        }
    }
}
