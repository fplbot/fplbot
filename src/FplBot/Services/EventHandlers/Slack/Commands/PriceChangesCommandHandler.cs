using Fpl.Client.Abstractions;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Services.WebApi.Slack.Extensions;
using MassTransit;

namespace FplBot.EventHandlers.Slack.Commands;

public class PriceChangesCommandHandler(
    ISlackWorkSpacePublisher workSpacePublisher,
    IGlobalSettingsClient globalSettingsClient)
    : IConsumer<ProcessPriceChangesCommand>
{
    public async Task Consume(ConsumeContext<ProcessPriceChangesCommand> context)
    {
        var command = context.Message;
        var globalSettings = await globalSettingsClient.GetGlobalSettings();
        var allPlayers = globalSettings!.Players;
        var teams = globalSettings.Teams;

        var priceChangedPlayers = allPlayers.Where(p => p.CostChangeEvent != 0 && p.IsRelevant())
            .Select(p =>
            {
                var t = teams.First(t => t.Code == p.TeamCode);
                return new PlayerWithPriceChange(p.Id, p.WebName ?? "", p.CostChangeEvent, p.NowCost, p.OwnershipPercentage, t.Id, t.ShortName ?? "");
            });

        var messageToSend = priceChangedPlayers.Any()
            ? Formatter.FormatPriceChanged(priceChangedPlayers)
            : "No relevant price changes yet";

        await workSpacePublisher.PublishToWorkspace(command.TeamId, command.Channel, messageToSend);
    }
}
