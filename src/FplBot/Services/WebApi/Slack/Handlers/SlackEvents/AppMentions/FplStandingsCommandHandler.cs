using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data.Slack;
using static FplBot.EventHandlers.Slack.Helpers.SlackInstallationExtensions;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Services.WebApi.Slack.Handlers.SlackEvents.AppMentions;

internal class FplStandingsCommandHandler : HandleAppMentionBase
{
    private readonly IGlobalSettingsClient _globalSettingsClient;
    private readonly ISlackTeamRepository _teamRepo;
    private readonly IPublishEndpoint _publishEndpoint;

    public FplStandingsCommandHandler(IGlobalSettingsClient globalSettingsClient, ISlackTeamRepository teamRepo, IPublishEndpoint publishEndpoint, ILogger<FplStandingsCommandHandler> logger)
    {
        _globalSettingsClient = globalSettingsClient;
        _teamRepo = teamRepo;
        _publishEndpoint = publishEndpoint;
    }

    public override string[] Commands => ["standings"];

    public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent appMentioned)
    {
        var installation = await _teamRepo.GetInstallation(eventMetadata.Team_Id);
        var settings =  await _globalSettingsClient.GetGlobalSettings();
        var gameweek = settings!.Gameweeks.GetCurrentGameweek();
        if (installation.HasChannelAndLeagueSetup())
        {
            var leagueId = (int)installation.PrimaryChannel()!.FollowedLeagueId!.Value;
            await _publishEndpoint.Publish(new PublishStandingsToSlackWorkspace(installation.TeamId, appMentioned.Channel, leagueId, gameweek!.Id));
        }

        return new EventHandledResponse("OK");
    }
    public override (string,string) GetHelpDescription() => (CommandsFormatted, "Get current league standings");
}
