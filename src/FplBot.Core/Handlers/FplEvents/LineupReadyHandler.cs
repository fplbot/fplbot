using System;
using System.Threading;
using System.Threading.Tasks;
using FplBot.Core.Extensions;
using FplBot.Core.Helpers;
using FplBot.Core.Models;
using FplBot.Data.Abstractions;
using FplBot.Data.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.Core.GameweekLifecycle.Handlers
{
    public class LineupReadyHandler : INotificationHandler<LineupReady>
    {
        private readonly ISlackTeamRepository _slackTeamRepo;
        private readonly ISlackClientBuilder _builder;
        private readonly ILogger<LineupReadyHandler> _logger;

        public LineupReadyHandler(ISlackTeamRepository slackTeamRepo, ISlackClientBuilder builder, ILogger<LineupReadyHandler> logger)
        {
            _slackTeamRepo = slackTeamRepo;
            _builder = builder;
            _logger = logger;
        }

        public async Task Handle(LineupReady notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling new lineups");
            var lineups = notification.Lineups;
            var slackTeams = await _slackTeamRepo.GetAllTeams();
            var firstMessage = $"*Lineups {lineups.HomeTeamNameAbbr}-{lineups.AwayTeamNameAbbr} ready* 👇";
            var formattedLineup = Formatter.FormatLineup(lineups);
            foreach (var slackTeam in slackTeams)
            {
                if (slackTeam.Subscriptions.ContainsSubscriptionFor(EventSubscription.Lineups))
                {
                    await PublishLineupToTeam(slackTeam);
                }
            }

            async Task PublishLineupToTeam(SlackTeam team)
            {
                var slackClient = _builder.Build(team.AccessToken);
                try
                {
                    var res = await slackClient.ChatPostMessage(team.FplBotSlackChannel, firstMessage);
                    if (res.Ok)
                    {
                        await slackClient.ChatPostMessage(new ChatPostMessageRequest
                        {
                            Channel = team.FplBotSlackChannel, thread_ts = res.ts, Text = formattedLineup, unfurl_links = "false"
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
