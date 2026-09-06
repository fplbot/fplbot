using Fpl.Search.Data.Repositories;
using FplBot.Data.Discord;
using FplBot.Data.Slack;
using FplBot.Discord.Data;
using FplBot.WebApi.Slack.Data;
using Microsoft.Extensions.Logging;

namespace FplBot.Tests;

public class SimpleLogger : ILogger<SlackTeamRepository>, ILogger<LeagueIndexRedisBookmarkProvider>, ILogger<DiscordGuildStore>, ILogger<TokenStore>, ILogger<DiscordGuildRepository>
{
    private readonly ITestOutputHelper _helper;

    public SimpleLogger(ITestOutputHelper helper)
    {
        _helper = helper;
    }

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
        _helper.WriteLine(formatter(state, exception));
    }
}
