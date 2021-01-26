using Fpl.Client.Abstractions;
using Fpl.Search.Searching;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    public class FplSearchHandler : HandleAppMentionBase
    {
        private readonly ISearchClient _searchClient;
        private readonly IGameweekClient _gameweekClient;
        private readonly ISlackWorkSpacePublisher _workSpacePublisher;
        private readonly ISlackTeamRepository _slackTeamRepo;
        private readonly ILeagueClient _leagueClient;
        private readonly IEntryClient _entryClient;
        private readonly ILogger<FplSearchHandler> _logger;


        public FplSearchHandler(
            ISearchClient searchClient,
            IGameweekClient gameweekClient,
            ISlackWorkSpacePublisher workSpacePublisher,
            ISlackTeamRepository slackTeamRepo,
            ILeagueClient leagueClient,
            IEntryClient entryClient,
            ILogger<FplSearchHandler> logger)
        {
            _searchClient = searchClient;
            _gameweekClient = gameweekClient;
            _workSpacePublisher = workSpacePublisher;
            _slackTeamRepo = slackTeamRepo;
            _leagueClient = leagueClient;
            _entryClient = entryClient;
            _logger = logger;
        }

        public override string[] Commands => new[] { "search" };

        public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent message)
        {
            var term = ParseArguments(message);

            string countryToBoost = await GetCountryToBoost(eventMetadata);

            var entriesTask = _searchClient.SearchForEntry(term, 10);
            var leaguesTask = _searchClient.SearchForLeague(term, 10, countryToBoost);

            var entries = await entriesTask;
            var leagues = await leaguesTask;

            var sb = new StringBuilder();
            sb.Append("Matching teams:\n");

            int? currentGameweek = null;
            if (entries.Any() || leagues.Any())
            {
                try
                {
                    var gameweeks = await _gameweekClient.GetGameweeks();
                    currentGameweek = gameweeks.GetCurrentGameweek().Id;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unable to obtain current gameweek when creating search result links");
                }
            }

            if (entries.Any())
            {
                sb.Append(Formatter.BulletPoints(entries.ExposedHits.Select(e => Formatter.FormatEntryItem(e, currentGameweek))));
                if (entries.HitCountExceedingExposedOnes > 0)
                {
                    sb.Append($"\n...and {entries.HitCountExceedingExposedOnes} more");
                }
            }
            else
            {
                sb.Append("Found no matching teams :shrug:");
            }

            sb.Append("\n\nMatching leagues:\n");

            if (leagues.Any())
            {
                sb.Append(Formatter.BulletPoints(leagues.ExposedHits.Select(e => Formatter.FormatLeagueItem(e, currentGameweek))));
                if (leagues.HitCountExceedingExposedOnes > 0)
                {
                    sb.Append($"\n...and {leagues.HitCountExceedingExposedOnes} more");
                }
            }
            else
            {
                sb.Append("Found no matching leagues :shrug:");
            }

            await _workSpacePublisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, sb.ToString());
            
            return new EventHandledResponse(sb.ToString());
        }

        private async Task<string> GetCountryToBoost(EventMetaData eventMetadata)
        {
            string countryToBoost = null;
            if (eventMetadata.Team_Id != null)
            {
                var team = await _slackTeamRepo.GetTeam(eventMetadata.Team_Id);

                if (team?.FplbotLeagueId != null)
                {
                    var league = await _leagueClient.GetClassicLeague((int) team.FplbotLeagueId);
                    var adminEntry = league?.Properties?.AdminEntry;

                    if (adminEntry != null)
                    {
                        var admin = await _entryClient.Get(adminEntry.Value);
                        if (admin != null)
                        {
                            countryToBoost = admin.PlayerRegionShortIso;
                        }
                    }
                }
            }

            return countryToBoost;
        }

        public override (string, string) GetHelpDescription() => ($"{CommandsFormatted} {{name}}", $"(:wrench: Beta) Search for teams or leagues. E.g. \"{CommandsFormatted} magnus carlsen\".");
    }
}