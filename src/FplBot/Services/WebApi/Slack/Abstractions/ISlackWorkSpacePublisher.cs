using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.WebApi.Slack.Abstractions;

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

}
