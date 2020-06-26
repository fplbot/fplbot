using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle
{
    public interface ISlackWorkSpacePublisher
    {
        Task PublishToAllWorkspaces(string msg);
        Task PublishUsingToken(string token, params string[] messages);
        Task PublishToSingleWorkspaceConnectedToLeague(int leagueId, params string[] messages);
    }
}