using Fpl.Client.Abstractions;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack.Commands;

public class InjuriesCommandHandler(
    ISlackWorkSpacePublisher workspacePublisher,
    IGlobalSettingsClient globalSettingsClient,
    IInjuredPlayersFinder injuredPlayersFinder)
    : IConsumer<ProcessInjuriesCommand>
{
    public async Task Consume(ConsumeContext<ProcessInjuriesCommand> context)
    {
        var command = context.Message;
        var globalSettings = await globalSettingsClient.GetGlobalSettings();

        var injuredPlayers = injuredPlayersFinder.FindInjuredPlayers(globalSettings?.Players ?? []);

        var textToSend = Formatter.GetInjuredPlayers(injuredPlayers);

        if (string.IsNullOrEmpty(textToSend))
        {
            return;
        }

        await workspacePublisher.PublishToWorkspace(command.TeamId, command.ChannelId, textToSend);
    }
}
