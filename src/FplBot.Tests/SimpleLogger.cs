using Fpl.Search.Data.Repositories;
using FplBot.Data.Discord;
using FplBot.Data.Slack;
using FplBot.Discord.Data;
using FplBot.WebApi.Slack.Data;
using Microsoft.Extensions.Logging;

namespace FplBot.Tests;

public class SimpleLogger(ITestOutputHelper helper) : ILogger<SlackTeamRepository>,
    ILogger<LeagueIndexRedisBookmarkProvider>, ILogger<DiscordGuildStore>, ILogger<TokenStore>,
    ILogger<DiscordGuildRepository>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        helper.WriteLine(formatter(state, exception));
    }
}
