using Fpl.Search.Searching;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    public class FplSearchHandler : HandleAppMentionBase
    {
        private readonly ISearchClient _searchClient;
        private readonly ISlackWorkSpacePublisher _workSpacePublisher;

        public FplSearchHandler(
            ISearchClient searchClient,
            ISlackWorkSpacePublisher workSpacePublisher)
        {
            _searchClient = searchClient;
            _workSpacePublisher = workSpacePublisher;
        }

        public override string Command => "search";

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
                sb.Append(Formatter.BulletPoints(entries.Select(Formatter.FormatEntryItem)));
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

        public override (string, string) GetHelpDescription() => ($"{Command} {{term}}", "Search for teams or leagues");
    }
}