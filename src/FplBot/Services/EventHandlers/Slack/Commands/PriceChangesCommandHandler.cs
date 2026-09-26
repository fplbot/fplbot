using Fpl.Client.Abstractions;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack.Commands;

public class PriceChangesCommandHandler(
    ISlackWorkSpacePublisher workSpacePublisher,
    IGlobalSettingsClient globalSettingsClient,
    IPriceChangedPlayersFinder priceChangedPlayersFinder)
    : IConsumer<ProcessPriceChangesCommand>
{
    public async Task Consume(ConsumeContext<ProcessPriceChangesCommand> context)
    {
        var command = context.Message;
        var globalSettings = await globalSettingsClient.GetGlobalSettings();

        var priceChangedPlayers = priceChangedPlayersFinder.FindPriceChangedPlayers(globalSettings!.Players, globalSettings.Teams);

        var messageToSend = priceChangedPlayers.Any()
            ? Formatter.FormatPriceChanged(priceChangedPlayers)
            : "No relevant price changes yet";

        await workSpacePublisher.PublishToWorkspace(command.TeamId, command.ChannelId, messageToSend);
    }
}
