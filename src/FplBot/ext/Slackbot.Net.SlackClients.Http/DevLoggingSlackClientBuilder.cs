namespace Slackbot.Net.SlackClients.Http;

/// <summary>
/// The single integration point for building an ISlackClient. Only registered in
/// Development (see AddDevAwareSlackClientBuilder), where it hands back a
/// DevLoggingSlackClient instead of a real one, so every caller (fulltime handler,
/// workspace publisher, admin pages, ...) automatically gets dev-safe behavior — no real
/// Slack API call, regardless of token — without needing to know about it. The token
/// itself is accepted but unused: no real client is ever constructed.
/// </summary>
public class DevLoggingSlackClientBuilder(ILoggerFactory loggerFactory) : ISlackClientBuilder
{
    public ISlackClient Build(string token)
    {
        return new DevLoggingSlackClient(loggerFactory.CreateLogger<DevLoggingSlackClient>());
    }
}
