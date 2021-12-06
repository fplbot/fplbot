using Fpl.Client.Models;

namespace FplBot.Slack.Extensions;

public static class TeamsExtensions
{
    public static Team Get(this ICollection<Team> teams, int? teamId)
    {
        return teamId.HasValue ? teams.SingleOrDefault(p => p.Id == teamId) : null;
    }
}
