using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FplBot.Core.Abstractions;
using FplBot.Core.Helpers;
using FplBot.Core.Models;
using Microsoft.Extensions.Logging;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;

namespace FplBot.Core.GameweekLifecycle.Handlers
{
    public class FixtureEventsHandler
    {
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ISlackTeamRepository _slackTeamRepo;
        private readonly ISlackClientBuilder _service;
        private readonly ILeagueEntriesByGameweek _leagueEntriesByGameweek;
        private readonly ITransfersByGameWeek _transfersByGameWeek;
        private readonly ILogger<FixtureEventsHandler> _logger;

        public FixtureEventsHandler(ISlackWorkSpacePublisher publisher, ISlackTeamRepository slackTeamRepo, ISlackClientBuilder service, ILeagueEntriesByGameweek leagueEntriesByGameweek, ITransfersByGameWeek transfersByGameWeek, ILogger<FixtureEventsHandler> logger)
        {
            _publisher = publisher;
            _slackTeamRepo = slackTeamRepo;
            _service = service;
            _leagueEntriesByGameweek = leagueEntriesByGameweek;
            _transfersByGameWeek = transfersByGameWeek;
            _logger = logger;
        }

        public async Task OnNewFixtureEvents(FixtureUpdates fixtureUpdates)
        {
            _logger.LogInformation("Handling new fixture events");
            var slackTeams = await _slackTeamRepo.GetAllTeams();

            foreach (var slackTeam in slackTeams)
            {
                try
                {
                    await HandleForTeam(fixtureUpdates,slackTeam);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
        }

        public async Task HandleForTeam(FixtureUpdates updates, SlackTeam slackTeam)
        {
            var slackUsers = await GetSlackUsers(slackTeam);
            var entries = await _leagueEntriesByGameweek.GetEntriesForGameweek(updates.CurrentGameweek, (int) slackTeam.FplbotLeagueId);
            var transfers = await _transfersByGameWeek.GetTransfersByGameweek(updates.CurrentGameweek, (int) slackTeam.FplbotLeagueId);
            var context = new GameweekLeagueContext
            {
                Players = updates.Players,
                Teams = updates.Teams,
                Users = slackUsers,
                GameweekEntries = entries,
                SlackTeam = slackTeam,
                TransfersForLeague = transfers
            };
            var formattedStr = GameweekEventsFormatter.FormatNewFixtureEvents(updates.Events.ToList(), context);
            await _publisher.PublishToWorkspace(slackTeam.TeamId, slackTeam.FplBotSlackChannel, formattedStr.ToArray());
        }

        private async Task<IEnumerable<User>> GetSlackUsers(SlackTeam t)
        {
            var slackClient = _service.Build(t.AccessToken);

            try
            {
                var usersResponse = await slackClient.UsersList();
                if (usersResponse.Ok)
                    return usersResponse.Members;
                return Enumerable.Empty<User>();

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return Enumerable.Empty<User>();
            }
        }
    }
}