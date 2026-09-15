using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data.Slack;
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
        var channel = installation.GetChannel(appMentioned.Channel);
        if (channel?.FollowedLeagueId is { } leagueId)
        {
            await _publishEndpoint.Publish(new PublishStandingsToSlackWorkspace(installation.Id, appMentioned.Channel, (int)leagueId.Value, gameweek!.Id));
        }

        return new EventHandledResponse("OK");
    }
    public override (string,string) GetHelpDescription() => (CommandsFormatted, "Get current league standings");
}
