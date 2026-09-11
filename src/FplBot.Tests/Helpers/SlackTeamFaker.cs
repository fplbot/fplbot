using Bogus;
using FplBot.Data.Slack;

namespace FplBot.Tests.Helpers;

public static class SlackTeamFaker
{
    private static readonly Faker<SlackTeam> Faker = new Faker<SlackTeam>()
        .RuleFor(t => t.TeamId, f => "T" + f.Random.Replace("##########"))
        .RuleFor(t => t.TeamName, f => f.Company.CompanyName())
        .RuleFor(t => t.AccessToken, f => "xoxb-" + f.Random.AlphaNumeric(24))
        .RuleFor(t => t.FplBotSlackChannel, f => "#" + f.Lorem.Slug(2))
        // A real, currently-valid FPL league — some handlers (e.g. captains) call the live FPL API
        // with this id, so it can't be random garbage that 404s.
        .RuleFor(t => t.FplbotLeagueId, _ => 15263)
        .RuleFor(t => t.Subscriptions, _ => new List<EventSubscription>());

    /// <summary>
    /// A fresh, uniquely-identified SlackTeam per call, so tests sharing a real backing
    /// store (Redis) never collide on the same key even when xUnit runs them concurrently.
    /// </summary>
    public static SlackTeam Generate() => Faker.Generate();
}
