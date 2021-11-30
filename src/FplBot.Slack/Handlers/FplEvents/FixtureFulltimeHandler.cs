using Fpl.Client.Abstractions;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Slack.Data.Abstractions;
using FplBot.Slack.Data.Models;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.Slack.Handlers.FplEvents;

public class FixtureFulltimeHandler : IHandleMessages<FixtureFinished>, IHandleMessages<PublishFulltimeMessageToSlackWorkspace>
{
    private readonly ISlackClientBuilder _builder;
    private readonly ISlackTeamRepository _slackTeamRepo;
    private readonly ILogger<FixtureFulltimeHandler> _logger;
    private readonly IGlobalSettingsClient _settingsClient;
    private readonly IFixtureClient _fixtureClient;

    public FixtureFulltimeHandler(ISlackClientBuilder builder, ISlackTeamRepository slackTeamRepo, ILogger<FixtureFulltimeHandler> logger, IGlobalSettingsClient settingsClient, IFixtureClient fixtureClient)
    {
        _builder = builder;
        _slackTeamRepo = slackTeamRepo;
        _logger = logger;
        _settingsClient = settingsClient;
        _fixtureClient = fixtureClient;
    }

    public async Task Handle(FixtureFinished message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Handling fixture full time");
        var teams = await _slackTeamRepo.GetAllTeams();
        var settings = await _settingsClient.GetGlobalSettings();
        var fixtures = await _fixtureClient.GetFixtures();
        var fplfixture = fixtures.FirstOrDefault(f => f.Id == message.FixtureId);
        var fixture = FixtureFulltimeModelBuilder.CreateFinishedFixture(settings.Teams, settings.Players, fplfixture);
        var title = $"*FT: {fixture.HomeTeam.ShortName} {fixture.Fixture.HomeTeamScore}-{fixture.Fixture.AwayTeamScore} {fixture.AwayTeam.ShortName}*";
        var threadMessage = Formatter.FormatProvisionalFinished(fixture);

        foreach (var slackTeam in teams)
        {
            if (slackTeam.HasRegisteredFor(EventSubscription.FixtureFullTime))
            {
                var options = new SendOptions();
                options.RequireImmediateDispatch();
                options.RouteToThisEndpoint();
                await context.Send(new PublishFulltimeMessageToSlackWorkspace(slackTeam.TeamId, title, threadMessage), options);
            }
        }
    }

    public async Task Handle(PublishFulltimeMessageToSlackWorkspace message, IMessageHandlerContext context)
    {
        var team = await _slackTeamRepo.GetTeam(message.WorkspaceId);
        var slackClient = _builder.Build(team.AccessToken);
        var res = await slackClient.ChatPostMessage(team.FplBotSlackChannel, message.Title);
        if(!string.IsNullOrEmpty(message.ThreadMessage) && res.Ok)
        {
            await slackClient.ChatPostMessage(new ChatPostMessageRequest
            {
                Channel = team.FplBotSlackChannel, thread_ts = res.ts, Text = message.ThreadMessage, unfurl_links = "false"
            });
        }
    }
}
