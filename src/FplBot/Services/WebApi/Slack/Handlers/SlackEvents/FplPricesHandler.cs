using Fpl.Client.Abstractions;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.WebApi.Slack.Abstractions;
using FplBot.WebApi.Slack.Extensions;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.WebApi.Slack.Handlers.SlackEvents;

internal class FplPricesHandler : HandleAppMentionBase
{
    private readonly ISlackWorkSpacePublisher _workSpacePublisher;
    private readonly IGlobalSettingsClient _globalSettingsClient;

    public FplPricesHandler(ISlackWorkSpacePublisher workSpacePublisher, IGlobalSettingsClient globalSettingsClient)
    {
        _workSpacePublisher = workSpacePublisher;
        _globalSettingsClient = globalSettingsClient;
    }

    public override string[] Commands => new[] { "pricechanges" };

    public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent message)
    {
        var globalSettings = await _globalSettingsClient.GetGlobalSettings();
        var allPlayers = globalSettings.Players;
        var teams = globalSettings.Teams;

        var priceChangedPlayers = allPlayers.Where(p => p.CostChangeEvent != 0 && p.IsRelevant())
            .Select(p =>
            {
                var t = teams.First(t => t.Code == p.TeamCode);
                return new PlayerWithPriceChange(p.Id, p.WebName, p.CostChangeEvent, p.NowCost, p.OwnershipPercentage, t.Id, t.ShortName);
            });
        if (priceChangedPlayers.Any())
        {

            var messageToSend = Formatter.FormatPriceChanged(priceChangedPlayers);
            await _workSpacePublisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, messageToSend);
        }
        else
        {
            await _workSpacePublisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, "No relevant price changes yet");
        }

        return new EventHandledResponse("Ok");
    }

    public override (string,string) GetHelpDescription() => (CommandsFormatted, "Displays players with recent price change");
}
