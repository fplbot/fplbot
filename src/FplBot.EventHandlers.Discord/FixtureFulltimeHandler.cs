using Fpl.Client.Abstractions;
using FplBot.Data.Discord;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.EventHandlers.Discord;

public class FixtureFulltimeHandler : IHandleMessages<FixtureFinished>
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

    public async Task Handle(FixtureFinished message, IMessageHandlerContext context)
    {
        var subs = await _teamRepo.GetAllGuildSubscriptions();
        var settings = await _settingsClient.GetGlobalSettings();
        var fixtures = await _fixtureClient.GetFixtures();
        var fplfixture = fixtures.FirstOrDefault(f => f.Id == message.FixtureId);
        var fixture = FixtureFulltimeModelBuilder.CreateFinishedFixture(settings.Teams, settings.Players, fplfixture);
        var title = $"*FT: {fixture.HomeTeam.ShortName} {fixture.Fixture.HomeTeamScore}-{fixture.Fixture.AwayTeamScore} {fixture.AwayTeam.ShortName}*";
        var threadMessage = Formatter.FormatProvisionalFinished(fixture);
        foreach (var sub in subs)
        {
            if (sub.Subscriptions.ContainsSubscriptionFor(EventSubscription.FixtureFullTime))
            {
                var options = new SendOptions();
                options.RequireImmediateDispatch();
                options.RouteToThisEndpoint();
                await context.Send(new PublishRichToGuildChannel(sub.GuildId, sub.ChannelId, $"ℹ️ {title}",$"{threadMessage}"), options);
            }

        }
    }
}
