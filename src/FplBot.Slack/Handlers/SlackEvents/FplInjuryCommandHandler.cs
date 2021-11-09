using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Formatting;
using FplBot.Slack.Abstractions;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Slack.Handlers.SlackEvents;

internal class FplInjuryCommandHandler : HandleAppMentionBase
{
    private readonly ISlackWorkSpacePublisher _workspacePublisher;
    private readonly IGlobalSettingsClient _globalSettingsClient;

    public FplInjuryCommandHandler(ISlackWorkSpacePublisher workspacePublisher, IGlobalSettingsClient globalSettingsClient)
    {
        _workspacePublisher = workspacePublisher;
        _globalSettingsClient = globalSettingsClient;
    }

    public override string[] Commands => new[] { "injuries" };

    public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent message)
    {
        var globalSettings = await _globalSettingsClient.GetGlobalSettings();

        var injuredPlayers = FindInjuredPlayers(globalSettings.Players);

        var textToSend = Formatter.GetInjuredPlayers(injuredPlayers);

        if (string.IsNullOrEmpty(textToSend))
        {
            return new EventHandledResponse("Not found");
        }
        await _workspacePublisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, textToSend);

        return new EventHandledResponse(textToSend);
    }


    private static IEnumerable<Player> FindInjuredPlayers(IEnumerable<Player> players)
    {
        return players.Where(p => p.OwnershipPercentage > 5 && IsInjured(p)).OrderByDescending(p => p.OwnershipPercentage);
    }

    private static bool IsInjured(Player player)
    {
        return (player.ChanceOfPlayingNextRound.HasValue && player.ChanceOfPlayingNextRound != 100);
    }

    public override (string,string) GetHelpDescription() => (CommandsFormatted, "See injured players owned by more than 5 %");
}