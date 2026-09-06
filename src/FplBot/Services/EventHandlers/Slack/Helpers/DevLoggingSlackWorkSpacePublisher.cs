using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.EventHandlers.Slack.Helpers;

public class DevLoggingSlackWorkSpacePublisher : ISlackWorkSpacePublisher
{
    private readonly SlackWorkSpacePublisher _inner;
    private readonly IHostEnvironment _env;
    private readonly ILogger<DevLoggingSlackWorkSpacePublisher> _logger;

    public DevLoggingSlackWorkSpacePublisher(SlackWorkSpacePublisher inner, IHostEnvironment env, ILogger<DevLoggingSlackWorkSpacePublisher> logger)
    {
        _inner = inner;
        _env = env;
        _logger = logger;
    }

    public async Task PublishToWorkspace(string teamId, string channel, params string[] messages)
    {
        if (!_env.IsDevelopment()) { await _inner.PublishToWorkspace(teamId, channel, messages); return; }
        foreach (var msg in messages)
            _logger.LogInformation("[DEV] Slack → {Team}/{Channel}\n{Message}", teamId, channel, msg);
    }

    public async Task PublishToWorkspace(string teamId, params ChatPostMessageRequest[] messages)
    {
        if (!_env.IsDevelopment()) { await _inner.PublishToWorkspace(teamId, messages); return; }
        foreach (var msg in messages)
            _logger.LogInformation("[DEV] Slack → {Channel}\n{Text}", msg.Channel, msg.Text);
    }
}
