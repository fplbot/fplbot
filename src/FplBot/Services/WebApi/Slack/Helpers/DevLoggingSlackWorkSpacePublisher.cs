using FplBot.WebApi.Slack.Abstractions;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.WebApi.Slack.Helpers;

internal class DevLoggingSlackWorkSpacePublisher(
    SlackWorkSpacePublisher inner,
    IHostEnvironment env,
    ILogger<DevLoggingSlackWorkSpacePublisher> logger)
    : ISlackWorkSpacePublisher
{
    public async Task PublishToAllWorkspaceChannels(string msg)
    {
        if (!env.IsDevelopment()) { await inner.PublishToAllWorkspaceChannels(msg); return; }
        logger.LogInformation("\n[DEV] Slack → All workspaces\n\n{Message}\n", msg);
    }

    public async Task PublishToWorkspace(string teamId, string channel, params string[] messages)
    {
        if (!env.IsDevelopment()) { await inner.PublishToWorkspace(teamId, channel, messages); return; }
        foreach (var msg in messages)
            logger.LogInformation("\n[DEV] Slack → {Team}/{Channel}\n\n{Message}\n", teamId, channel, msg);
    }

    public async Task PublishToWorkspace(string teamId, params ChatPostMessageRequest[] messages)
    {
        if (!env.IsDevelopment()) { await inner.PublishToWorkspace(teamId, messages); return; }
        foreach (var msg in messages)
            logger.LogInformation("\n[DEV] Slack → {Channel}\n\n{Text}\n", msg.Channel, msg.Text);
    }
}
