using System;
using Fpl.Search.Searching;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Extensions;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    public class FplSearchHandler : HandleAppMentionBase
    {
        private readonly ISearchClient _searchClient;
        private readonly IGameweekClient _gameweekClient;
        private readonly ISlackWorkSpacePublisher _workSpacePublisher;
        private readonly ILogger<FplSearchHandler> _logger;


        public FplSearchHandler(
            ISearchClient searchClient,
            IGameweekClient gameweekClient,
            ISlackWorkSpacePublisher workSpacePublisher, 
            ILogger<FplSearchHandler> logger)
        {
            _searchClient = searchClient;
            _gameweekClient = gameweekClient;
            _workSpacePublisher = workSpacePublisher;
            _logger = logger;
        }

        public override string[] Commands => new[] { "search" };

        public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent message)
        {
            var term = ParseArguments(message);

            var entriesTask = _searchClient.SearchForEntry(term, 10);
            var leaguesTask = _searchClient.SearchForLeague(term, 10);

            var entries = await entriesTask;
            var leagues = await leaguesTask;

            var sb = new StringBuilder();
            sb.Append("Matching teams:\n");

            if (entries.Count > 0)
            {
                int? currentGameweek = null;
                try
                {
                    var gameweeks = await _gameweekClient.GetGameweeks();
                    currentGameweek = gameweeks.GetCurrentGameweek().Id;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unable to obtain current gameweek when creating search result links");
                }
             
                sb.Append(Formatter.BulletPoints(entries.Select(e => Formatter.FormatEntryItem(e, currentGameweek))));
            }
            else
            {
                sb.Append("Found no matching teams :shrug:");
            }

            sb.Append("\n\nMatching leagues:\n");

            if (leagues.Count > 0)
            {
                sb.Append(Formatter.BulletPoints(leagues.Select(Formatter.FormatLeagueItem)));
            }
            else
            {
                sb.Append("Found no matching leagues :shrug:");
            }

            await _workSpacePublisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, sb.ToString());
            
            return new EventHandledResponse(sb.ToString());
        }

        public override (string, string) GetHelpDescription() => ($"{CommandsFormatted} {{name}}", $"(:wrench: Beta) Search for teams or leagues. E.g. \"{CommandsFormatted} magnus carlsen\".");
    }
}