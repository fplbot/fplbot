using System;
using System.Threading;
using System.Threading.Tasks;
using Fpl.Data;
using Fpl.Data.Repositories;
using FplBot.Core.Abstractions;
using FplBot.Core.Extensions;
using FplBot.Core.Helpers;
using FplBot.Core.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.Core.GameweekLifecycle.Handlers
{
    public class FixtureFulltimeHandler : INotificationHandler<FixturesFinished>
    {
        private readonly ISlackClientBuilder _builder;
        private readonly ISlackTeamRepository _slackTeamRepo;
        private readonly ILogger<FixtureFulltimeHandler> _logger;

        public FixtureFulltimeHandler(ISlackClientBuilder builder, ISlackTeamRepository slackTeamRepo, ILogger<FixtureFulltimeHandler> logger)
        {
            _builder = builder;
            _slackTeamRepo = slackTeamRepo;
            _logger = logger;
        }

        public async Task Handle(FixturesFinished notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling fixture full time");
            var teams = await _slackTeamRepo.GetAllTeams();
            
            foreach (var slackTeam in teams)
            {
                if (slackTeam.Subscriptions.ContainsSubscriptionFor(EventSubscription.FixtureFullTime))
                {
                    foreach (var fixture in notification.FinishedFixture)
                    {
                        var title = $"*FT: {fixture.HomeTeam.ShortName} {fixture.Fixture.HomeTeamScore}-{fixture.Fixture.AwayTeamScore} {fixture.AwayTeam.ShortName}*";
                        var formatted = Formatter.FormatProvisionalFinished(fixture);
                        await PublishFulltimeMessage(slackTeam, title, formatted);
                    }
                }
            }
            
            async Task PublishFulltimeMessage(SlackTeam team, string title, string thread)
            {
                var slackClient = _builder.Build(team.AccessToken);
                try
                {
                    var res = await slackClient.ChatPostMessage(team.FplBotSlackChannel, title);
                    if (res.Ok)
                    {
                        await slackClient.ChatPostMessage(new ChatPostMessageRequest
                        {
                            Channel = team.FplBotSlackChannel, thread_ts = res.ts, Text = thread, unfurl_links = "false"
                        });
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, e.Message);
                }
            }
        }
    }
}