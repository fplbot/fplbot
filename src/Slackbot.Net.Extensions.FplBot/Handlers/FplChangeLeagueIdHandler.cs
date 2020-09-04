using Fpl.Client.Abstractions;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    public class FplChangeLeagueIdHandler : IHandleEvent
    {
        private readonly ISlackTeamRepository _slackTeamRepository;
        private readonly ILeagueClient _leagueClient;

        public FplChangeLeagueIdHandler(ISlackTeamRepository slackTeamRepository, ILeagueClient leagueClient)
        {
            _slackTeamRepository = slackTeamRepository;
            _leagueClient = leagueClient;
        }

        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, SlackEvent slackEvent)
        {
            var message = slackEvent as AppMentionEvent;

            var newLeagueId = ParseLeagueId(message);
            await _slackTeamRepository.UpdateLeagueId(eventMetadata.Team_Id, long.Parse(newLeagueId));

            var league = await _leagueClient.GetClassicLeague(int.Parse(newLeagueId));

            return new EventHandledResponse($"Thanks! You're now following {league.Properties.Name}.");
        }

        private static string ParseLeagueId(AppMentionEvent message)
        {
            return new MessageHelper().ExtractArgs(message.Text, "updateleagueid {args}");
        }

        public bool ShouldHandle(SlackEvent slackEvent) => slackEvent is AppMentionEvent @event && @event.Text.Contains("updateleagueid");

        public (string, string) GetHelpDescription() => ("updateleagueid <new league id>", "Change which league to follow");
    }
}