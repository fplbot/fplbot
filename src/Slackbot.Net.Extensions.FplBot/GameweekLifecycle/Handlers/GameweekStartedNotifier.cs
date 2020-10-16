using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.RecurringActions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    internal class GameweekStartedNotifier : IHandleGameweekStarted
    {
        private readonly ICaptainsByGameWeek _captainsByGameweek;
        private readonly ITransfersByGameWeek _transfersByGameweek;
        private readonly ISlackTeamRepository _slackTeamRepo;
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly IFetchFplbotSetup _teamRepo;
        private readonly ILogger<GameweekStartedNotifier> _logger;

        public GameweekStartedNotifier(ICaptainsByGameWeek captainsByGameweek, 
            ITransfersByGameWeek transfersByGameweek,
            ISlackTeamRepository slackTeamRepo,
            ISlackWorkSpacePublisher publisher, 
            IFetchFplbotSetup teamsRepo, 
            ILogger<GameweekStartedNotifier> logger)
        {
            _captainsByGameweek = captainsByGameweek;
            _transfersByGameweek = transfersByGameweek;
            _slackTeamRepo = slackTeamRepo;
            _publisher = publisher;
            _teamRepo = teamsRepo;
            _logger = logger;
        }

        public async Task HandleGameweekStarted(int newGameweek)
        {
            await _publisher.PublishToAllWorkspaceChannels($"Gameweek {newGameweek}!");
            var allTeams = await _slackTeamRepo.GetAllTeamsAsync();

            foreach (var team in allTeams)
            {
                try
                {
                    var messages = new List<string>();

                    if (team.FplBotEventSubscriptions.ContainsSubscriptionFor(EventSubscription.Captains))
                    {
                        messages.Add(await _captainsByGameweek.GetCaptainsByGameWeek(newGameweek, (int)team.FplbotLeagueId));
                        messages.Add(await _captainsByGameweek.GetCaptainsChartByGameWeek(newGameweek, (int)team.FplbotLeagueId));
                    }

                    if (team.FplBotEventSubscriptions.ContainsSubscriptionFor(EventSubscription.Transfers))
                    {
                        messages.Add(await _transfersByGameweek.GetTransfersByGameweekTexts(newGameweek, (int)team.FplbotLeagueId));
                    }

                    await _publisher.PublishToWorkspaceChannelUsingToken(team.AccessToken, messages.ToArray());
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
              
            }
        }
    }
}