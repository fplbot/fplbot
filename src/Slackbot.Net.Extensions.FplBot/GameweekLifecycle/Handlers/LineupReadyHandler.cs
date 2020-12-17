using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.Models;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    public class LineupReadyHandler
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
        
        public async Task HandleLineupReady(Lineups lineups)
        {
            _logger.LogInformation("Handling new lineups");
            var slackTeams = await _slackTeamRepo.GetAllTeams();
            var firstMessage = $"*Lineups {lineups.HomeTeamNameAbbr}-{lineups.AwayTeamNameAbbr} ready* 👇";
            var formattedLineup = Formatter.FormatLineup(lineups);
            foreach (var slackTeam in slackTeams)
            {
                await PublishLineupToTeam(slackTeam);
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