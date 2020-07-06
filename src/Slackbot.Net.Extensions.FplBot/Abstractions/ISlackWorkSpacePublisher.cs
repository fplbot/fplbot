using System.Threading.Tasks;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle
{
    public interface ISlackWorkSpacePublisher
    {
        Task PublishTAllWorkspaceChannels(string msg);
        Task PublishToWorkspaceChannelUsingToken(string token, params string[] messages);
        Task PublishToWorkspaceChannelConnectedToLeague(int leagueId, params string[] messages);
        Task PublishToWorkspace(string teamId, string channel, params string[] messages);
        Task PublishToWorkspace(string teamId, params ChatPostMessageRequest[] message);

    }
}