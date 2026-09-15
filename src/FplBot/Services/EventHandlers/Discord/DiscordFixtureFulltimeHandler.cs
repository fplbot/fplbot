using Fpl.Client.Abstractions;
using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class DiscordFixtureFulltimeHandler(
    IGuildRepository teamRepo,
    ILogger<DiscordFixtureFulltimeHandler> logger,
    IGlobalSettingsClient settingsClient,
    IFixtureClient fixtureClient,
    ILiveClient liveClient)
    : IConsumer<FixtureFinished>
{
    private readonly ILogger<DiscordFixtureFulltimeHandler> _logger = logger;

    public async Task Consume(ConsumeContext<FixtureFinished> context)
    {
        var message = context.Message;
        var subscribedChannels = await teamRepo.GetChannelsSubscribedTo(FplEvent.FixtureFullTime);
        var settings = await settingsClient.GetGlobalSettings();
        var fixtures = await fixtureClient.GetFixtures() ?? [];
        var fplfixture = fixtures.FirstOrDefault(f => f.Id == message.FixtureId);
        if (fplfixture == null)
        {
            _logger.LogWarning("Could not find fixture {FixtureId} in FPL API", message.FixtureId);
            return;
        }
        var liveItems = fplfixture.Event.HasValue
            ? await liveClient.GetLiveItems(fplfixture.Event.Value, isOngoingGameweek: true)
            : null;
        var fixture = FixtureFulltimeModelBuilder.CreateFinishedFixture(settings?.Teams ?? [], settings?.Players ?? [], fplfixture, liveItems);
        var title = $"*FT: {fixture.HomeTeam.ShortName} {fixture.Fixture.HomeTeamScore}-{fixture.Fixture.AwayTeamScore} {fixture.AwayTeam.ShortName}*";
        var threadMessage = Formatter.FormatProvisionalFinished(fixture);
        foreach (var (guildId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishRichToGuildChannel(guildId, channelId, $"ℹ️ {title}", $"{threadMessage}"));
        }
    }
}
