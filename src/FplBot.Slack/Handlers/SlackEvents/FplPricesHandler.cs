using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using FplBot.Formatting;
using FplBot.Slack.Abstractions;
using FplBot.Slack.Extensions;
using FplBot.Slack.Helpers.Formatting;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Slack.Handlers.SlackEvents
{
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

            var priceChangedPlayers = allPlayers.Where(p => p.CostChangeEvent != 0 && p.IsRelevant());
            if (priceChangedPlayers.Any())
            {
                var messageToSend = Formatter.FormatPriceChanged(priceChangedPlayers, teams);
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
}
