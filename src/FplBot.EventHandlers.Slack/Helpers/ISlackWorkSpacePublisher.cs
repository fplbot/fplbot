using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.EventHandlers.Slack.Helpers;

public interface ISlackWorkSpacePublisher
{

    /// <summary>
    /// Publishes to single workspaces to the channel provided
    /// </summary>
    Task PublishToWorkspace(string teamId, string channel, params string[] messages);

    /// <summary>
    /// Publishes to single workspaces to the channel provided
    /// </summary>
    Task PublishToWorkspace(string teamId, params ChatPostMessageRequest[] message);
}
