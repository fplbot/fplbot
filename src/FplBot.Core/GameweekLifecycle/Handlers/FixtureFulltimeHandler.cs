using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.Models;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    public class FixtureFulltimeHandler
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

        public async Task OnFixtureFulltime(IEnumerable<FinishedFixture> provisionalFixturesFinished)
        {
            _logger.LogInformation("Handling fixture full time");

            var teams = await _slackTeamRepo.GetAllTeams();
            
            foreach (var slackTeam in teams)
            {
                if (slackTeam.Subscriptions.ContainsSubscriptionFor(EventSubscription.FixtureFullTime))
                {
                    foreach (var fixture in provisionalFixturesFinished)
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