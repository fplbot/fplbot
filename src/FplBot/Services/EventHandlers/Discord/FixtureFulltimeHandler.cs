using Fpl.Client.Abstractions;
using FplBot.Data.Discord;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class FixtureFulltimeHandler : IConsumer<FixtureFinished>
{
    private readonly IGuildRepository _teamRepo;
    private readonly ILogger<NearDeadlineHandler> _logger;
    private readonly IGlobalSettingsClient _settingsClient;
    private readonly IFixtureClient _fixtureClient;

    public FixtureFulltimeHandler(IGuildRepository teamRepo, ILogger<NearDeadlineHandler> logger, IGlobalSettingsClient settingsClient, IFixtureClient fixtureClient)
    {
        _teamRepo = teamRepo;
        _logger = logger;
        _settingsClient = settingsClient;
        _fixtureClient = fixtureClient;
    }

    public async Task Consume(ConsumeContext<FixtureFinished> context)
    {
        var message = context.Message;
        var subs = await _teamRepo.GetAllGuildSubscriptions();
        var settings = await _settingsClient.GetGlobalSettings();
        var fixtures = await _fixtureClient.GetFixtures() ?? new List<Fpl.Client.Models.Fixture>();
        var fplfixture = fixtures.FirstOrDefault(f => f.Id == message.FixtureId)!;
        var fixture = FixtureFulltimeModelBuilder.CreateFinishedFixture(settings?.Teams ?? new List<Fpl.Client.Models.Team>(), settings?.Players ?? new List<Fpl.Client.Models.Player>(), fplfixture);
        var title = $"*FT: {fixture.HomeTeam.ShortName} {fixture.Fixture.HomeTeamScore}-{fixture.Fixture.AwayTeamScore} {fixture.AwayTeam.ShortName}*";
        var threadMessage = Formatter.FormatProvisionalFinished(fixture);
        foreach (var sub in subs)
        {
            if (sub.Subscriptions.ContainsSubscriptionFor(EventSubscription.FixtureFullTime))
            {
                await context.Publish(new PublishRichToGuildChannel(sub.GuildId, sub.ChannelId, $"ℹ️ {title}",$"{threadMessage}"));
            }

        }
    }
}
