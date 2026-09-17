using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack.Commands;

public class InjuriesCommandHandler(
    ISlackWorkSpacePublisher workspacePublisher,
    IGlobalSettingsClient globalSettingsClient)
    : IConsumer<ProcessInjuriesCommand>
{
    public async Task Consume(ConsumeContext<ProcessInjuriesCommand> context)
    {
        var command = context.Message;
        var globalSettings = await globalSettingsClient.GetGlobalSettings();

        var injuredPlayers = FindInjuredPlayers(globalSettings?.Players ?? []);

        var textToSend = Formatter.GetInjuredPlayers(injuredPlayers);

        if (string.IsNullOrEmpty(textToSend))
        {
            return;
        }

        await workspacePublisher.PublishToWorkspace(command.TeamId, command.Channel, textToSend);
    }

    private static IEnumerable<Player> FindInjuredPlayers(IEnumerable<Player> players)
    {
        return players.Where(p => p.OwnershipPercentage > 5 && IsInjured(p)).OrderByDescending(p => p.OwnershipPercentage);
    }

    private static bool IsInjured(Player player)
    {
        return player.ChanceOfPlayingNextRound.HasValue && player.ChanceOfPlayingNextRound != 100;
    }
}
