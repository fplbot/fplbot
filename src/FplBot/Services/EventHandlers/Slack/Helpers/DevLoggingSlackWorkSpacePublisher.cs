using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.EventHandlers.Slack.Helpers;

public class DevLoggingSlackWorkSpacePublisher(
    SlackWorkSpacePublisher inner,
    IHostEnvironment env,
    ILogger<DevLoggingSlackWorkSpacePublisher> logger)
    : ISlackWorkSpacePublisher
{
    public async Task PublishToWorkspace(string teamId, string channel, params string[] messages)
    {
        if (!env.IsDevelopment()) { await inner.PublishToWorkspace(teamId, channel, messages); return; }
        foreach (var msg in messages)
            logger.LogInformation("[DEV] Slack → {Team}/{Channel}\n{Message}", teamId, channel, msg);
    }

    public async Task PublishToWorkspace(string teamId, params ChatPostMessageRequest[] messages)
    {
        if (!env.IsDevelopment()) { await inner.PublishToWorkspace(teamId, messages); return; }
        foreach (var msg in messages)
            logger.LogInformation("[DEV] Slack → {Channel}\n{Text}", msg.Channel, msg.Text);
    }
}
