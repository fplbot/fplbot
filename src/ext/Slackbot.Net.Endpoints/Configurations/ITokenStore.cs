using System.Collections.Generic;
using System.Threading.Tasks;

namespace Slackbot.Net.Abstractions.Hosting
{
    /// <summary>
    /// Provider of tokens from all workspaces that have installed your distributed Slack app
    /// </summary>
    public interface ITokenStore
    {
        Task<IEnumerable<string>> GetTokens();
        Task<string> GetTokenByTeamId(string teamId);
        Task Delete(string token);
        Task Insert(Workspace slackTeam);
    }

    public record Workspace(string TeamId, string TeamName, string Scope, string Token);
}
