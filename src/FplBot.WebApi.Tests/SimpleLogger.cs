using Fpl.Search.Data.Repositories;
using FplBot.Data.Discord;
using FplBot.Data.Slack;
using FplBot.Discord.Data;
using FplBot.VerifiedEntries.Data.Repositories;
using FplBot.WebApi.Slack.Data;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Abstractions.Hosting;
using Xunit.Abstractions;

namespace FplBot.WebApi.Tests;

public class SimpleLogger : ILogger<SlackTeamRepository>, ILogger<LeagueIndexRedisBookmarkProvider>, ILogger<VerifiedEntriesRepository>, ILogger<DiscordGuildStore>, ILogger<TokenStore>, ILogger<DiscordGuildRepository>
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
