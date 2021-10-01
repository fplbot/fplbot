using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Search.Models;
using Fpl.Search.Searching;
using FplBot.Slack.Abstractions;
using FplBot.Slack.Data.Abstractions;
using FplBot.Slack.Data.Models;
using FplBot.Slack.Extensions;
using FplBot.Slack.Helpers.Formatting;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Slack.Handlers.SlackEvents
{
    public class FplSearchHandler : HandleAppMentionBase
    {
        private readonly ISearchService _searchService;
        private readonly IGlobalSettingsClient _globalSettingsClient;
        private readonly ISlackWorkSpacePublisher _workSpacePublisher;
        private readonly ISlackTeamRepository _slackTeamRepo;
        private readonly ILeagueClient _leagueClient;
        private readonly IEntryClient _entryClient;
        private readonly ILogger<FplSearchHandler> _logger;


        public FplSearchHandler(
            ISearchService searchService,
            IGlobalSettingsClient globalSettingsClient,
            ISlackWorkSpacePublisher workSpacePublisher,
            ISlackTeamRepository slackTeamRepo,
            ILeagueClient leagueClient,
            IEntryClient entryClient,
            ILogger<FplSearchHandler> logger)
        {
            _searchService = searchService;
            _globalSettingsClient = globalSettingsClient;
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

            SlackTeam slackTeam = null;
            try
            {
                slackTeam = await _slackTeamRepo.GetTeam(eventMetadata.Team_Id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to get team {teamId} during search.", eventMetadata.Team_Id);
            }

            string countryToBoost = await GetCountryToBoost(slackTeam);

            var searchMetaData = GetSearchMetaData(slackTeam, message);

            var entriesTask = _searchService.SearchForEntry(term, 0, 10, searchMetaData);
            var leaguesTask = _searchService.SearchForLeague(term, 0, 10, searchMetaData, countryToBoost);

            var entries = await entriesTask;
            var leagues = await leaguesTask;

            var sb = new StringBuilder();
            sb.Append("Matching teams:\n");

            int? currentGameweek = null;
            if (entries.Any() || leagues.Any())
            {
                try
                {
                    var globalSettings = await _globalSettingsClient.GetGlobalSettings();
                    var gameweeks = globalSettings.Gameweeks;
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

        private static SearchMetaData GetSearchMetaData(SlackTeam slackTeam, AppMentionEvent message)
        {
            var metaData = new SearchMetaData
            {
                Team = slackTeam?.TeamId, FollowingFplLeagueId = slackTeam?.FplbotLeagueId.ToString(), Actor = message.User,
                Client = QueryClient.Slack
            };
            return metaData;
        }

        private async Task<string> GetCountryToBoost(SlackTeam slackTeam)
        {
            string countryToBoost = null;
            if (slackTeam?.FplbotLeagueId != null)
            {
                var league = await _leagueClient.GetClassicLeague(slackTeam.FplbotLeagueId.Value);
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

            return countryToBoost;
        }

        public override (string, string) GetHelpDescription() => ($"{CommandsFormatted} {{name}}", $"(:wrench: Beta) Search for teams or leagues. E.g. \"{CommandsFormatted} magnus carlsen\".");
    }
}
