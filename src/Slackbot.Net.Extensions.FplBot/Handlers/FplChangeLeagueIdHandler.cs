using System;
using System.Net.Http;
using Fpl.Client.Abstractions;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System.Threading.Tasks;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    public class FplChangeLeagueIdHandler : IHandleEvent
    {
        private readonly ISlackTeamRepository _slackTeamRepository;
        private readonly ILeagueClient _leagueClient;
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ILogger<FplChangeLeagueIdHandler> _logger;

        public FplChangeLeagueIdHandler(ISlackTeamRepository slackTeamRepository, ILeagueClient leagueClient, ISlackWorkSpacePublisher publisher, ILogger<FplChangeLeagueIdHandler> logger)
        {
            _slackTeamRepository = slackTeamRepository;
            _leagueClient = leagueClient;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, SlackEvent slackEvent)
        {
            var message = slackEvent as AppMentionEvent;

            var newLeagueId = ParseLeagueId(message);

            if (string.IsNullOrEmpty(newLeagueId))
            {
                var help = $"No leagueId provided. Usage: `@fplbot updateleagueid 123`";
                await _publisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, help);
                return new EventHandledResponse(help);
            }

            var couldParse = int.TryParse(newLeagueId, out var theLeagueId);
            
            if (!couldParse)
            {
                var res = $"Could not update league to id '{newLeagueId}'. Make sure it's a valid number.";
                await _publisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, res);
                return new EventHandledResponse(res);
            }

            var failure = $"Could not find league {newLeagueId} :/ Could you find it at https://fantasy.premierleague.com/leagues/{newLeagueId}/standings/c ?";
            try
            {
                var league = await _leagueClient.GetClassicLeague(theLeagueId);
                
                if (league?.Properties != null)
                {
                    await _slackTeamRepository.UpdateLeagueId(eventMetadata.Team_Id, theLeagueId);
                    var success = $"Thanks! You're now following the '{league.Properties.Name}' league (leagueId: {theLeagueId})";
                    await _publisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, success);
                    return new EventHandledResponse(success);    
                }
                await _publisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, failure);
                return new EventHandledResponse(failure);
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e.Message, e);
                return new EventHandledResponse(failure);
            }
        }

        private static string ParseLeagueId(AppMentionEvent message)
        {
            return new MessageHelper().ExtractArgs(message.Text, "updateleagueid {args}");
        }

        public bool ShouldHandle(SlackEvent slackEvent) => slackEvent is AppMentionEvent @event && @event.Text.Contains("updateleagueid");

        public (string, string) GetHelpDescription() => ("updateleagueid <new league id>", "Change which league to follow");
    }
}