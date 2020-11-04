using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.Models;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    public class FixtureEventsHandlerV2
    {
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ISlackTeamRepository _slackTeamRepo;
        private readonly IPlayerClient _playerClient;
        private readonly ITeamsClient _teamsClient;
        private readonly ISlackClientBuilder _service;
        private readonly ILeagueEntriesByGameweek _leagueEntriesByGameweek;
        private readonly ITransfersByGameWeek _transfersByGameWeek;
        private readonly ILogger<FixtureEventsHandlerV2> _logger;

        public FixtureEventsHandlerV2(ISlackWorkSpacePublisher publisher, ISlackTeamRepository slackTeamRepo, IPlayerClient playerClient, ITeamsClient teamsClient, ISlackClientBuilder service, ILeagueEntriesByGameweek leagueEntriesByGameweek, ITransfersByGameWeek transfersByGameWeek, ILogger<FixtureEventsHandlerV2> logger)
        {
            _publisher = publisher;
            _slackTeamRepo = slackTeamRepo;
            _playerClient = playerClient;
            _teamsClient = teamsClient;
            _service = service;
            _leagueEntriesByGameweek = leagueEntriesByGameweek;
            _transfersByGameWeek = transfersByGameWeek;
            _logger = logger;
        }

        public async Task OnNewFixtureEvents(int currentGameweek, IEnumerable<FixtureEvents> newEvents)
        {
            var slackTeams = await _slackTeamRepo.GetAllTeams();
            var players = await _playerClient.GetAllPlayers();
            var teams = await _teamsClient.GetAllTeams();
            
            foreach (var slackTeam in slackTeams)
            {
                try
                {
                    await HandleForTeam(currentGameweek, newEvents, slackTeam, players, teams);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
        }

        private async Task HandleForTeam(int currentGameweek, IEnumerable<FixtureEvents> newEvents, SlackTeam slackTeam, ICollection<Player> players, ICollection<Team> teams)
        {
            var slackUsers = await GetSlackUsers(slackTeam);
            var entries = await _leagueEntriesByGameweek.GetEntriesForGameweek(currentGameweek, (int) slackTeam.FplbotLeagueId);
            var transfers = await _transfersByGameWeek.GetTransfersByGameweek(currentGameweek, (int) slackTeam.FplbotLeagueId);
            var context = new GameweekLeagueContext
            {
                Players = players,
                Teams = teams,
                Users = slackUsers,
                CurrentGameweek = currentGameweek,
                GameweekEntries = entries,
                SlackTeam = slackTeam,
                TransfersForLeague = transfers,
            };
            var formattedEvents = GameweekEventsFormatter.FormatNewFixtureEvents(newEvents.ToList(), context);
            
            // beta-testing
            if (slackTeam.TeamId == "T0A9QSU83")
            {
                await _publisher.PublishToWorkspace(context.SlackTeam.TeamId, "#johntest", formattedEvents.ToArray());
            }
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