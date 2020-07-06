using System.Threading.Tasks;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    public interface ISlackWorkSpacePublisher
    {
        /// <summary>
        /// Publishes to the install-channel of ALL installed workspaces
        /// </summary>
        Task PublishToAllWorkspaceChannels(string msg);
        
        /// <summary>
        /// Publishes to the install-channel of a single workspaces connected to the token
        /// </summary>
        Task PublishToWorkspaceChannelUsingToken(string token, params string[] messages);
        
        /// <summary>
        /// Publishes to the install-channel of a single workspaces connected to the league
        /// </summary>
        Task PublishToWorkspaceChannelConnectedToLeague(int leagueId, params string[] messages);
        
        /// <summary>
        /// Publishes to single workspaces to the channel provided
        /// </summary>
        Task PublishToWorkspace(string teamId, string channel, params string[] messages);
        
        /// <summary>
        /// Publishes to single workspaces to the channel provided
        /// </summary>
        Task PublishToWorkspace(string teamId, params ChatPostMessageRequest[] message);

    }
}