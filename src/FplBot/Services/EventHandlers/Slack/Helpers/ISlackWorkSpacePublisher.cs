using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;
using Slackbot.Net.SlackClients.Http.Models.Responses.ChatPostMessage;

namespace FplBot.EventHandlers.Slack.Helpers;

public interface ISlackWorkSpacePublisher
{
    /// <summary>
    /// Publishes to the install-channel of ALL installed workspaces
    /// </summary>
    Task PublishToAllWorkspaceChannels(string msg);

    /// <summary>
    /// Publishes to single workspaces to the channel provided
    /// </summary>
    Task PublishToWorkspace(string teamId, string channel, params string[] messages);

    /// <summary>
    /// Publishes to single workspaces to the channel provided
    /// </summary>
    Task PublishToWorkspace(string teamId, params ChatPostMessageRequest[] message);

    /// <summary>
    /// Publishes a single message, handing back the Slack response so the caller can thread
    /// a follow-up message on its timestamp. Null when the post did not go through.
    /// </summary>
    Task<ChatPostMessageResponse?> PublishToWorkspaceWithResponse(string teamId, ChatPostMessageRequest message);
}
