namespace Slackbot.Net.SlackClients.Http;

/// <summary>
/// The single integration point for building an ISlackClient. Only registered in
/// Development (see AddDevAwareSlackClientBuilder), where it wraps the built client
/// in a DevLoggingSlackClient so every caller (fulltime handler, workspace publisher,
/// admin pages, ...) automatically gets dev-safe behavior without needing to know about it.
/// </summary>
public class DevLoggingSlackClientBuilder(SlackClientBuilder inner, ILoggerFactory loggerFactory) : ISlackClientBuilder
{
    public ISlackClient Build(string token)
    {
        var real = inner.Build(token);
        return new DevLoggingSlackClient(real, loggerFactory.CreateLogger<DevLoggingSlackClient>());
    }
}
