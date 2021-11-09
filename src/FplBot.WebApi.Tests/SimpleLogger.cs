using Fpl.Search.Data.Repositories;
using FplBot.Discord.Data;
using FplBot.Slack.Data.Repositories.Redis;
using FplBot.VerifiedEntries.Data.Repositories;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace FplBot.WebApi.Tests;

public class SimpleLogger : ILogger<SlackTeamRepository>, ILogger<LeagueIndexRedisBookmarkProvider>, ILogger<VerifiedEntriesRepository>, ILogger<DiscordGuildStore>
{
    private readonly ITestOutputHelper _helper;

    public SimpleLogger(ITestOutputHelper helper)
    {
        _helper = helper;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        throw new NotImplementedException();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        _helper.WriteLine(formatter(state, exception));
    }
}