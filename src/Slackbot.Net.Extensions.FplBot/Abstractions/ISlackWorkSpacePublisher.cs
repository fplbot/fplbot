using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle
{
    public interface ISlackWorkSpacePublisher
    {
        Task PublishTAllWorkspaceChannels(string msg);
        Task PublishToWorkspaceChannelUsingToken(string token, params string[] messages);
        Task PublishToWorkspaceChannelConnectedToLeague(int leagueId, params string[] messages);
        Task PublishToWorkspace(string teamId, string channel, params string[] messages);
    }
}