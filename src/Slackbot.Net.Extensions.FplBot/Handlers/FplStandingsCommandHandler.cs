using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplStandingsCommandHandler : IHandleEvent
    {
        private readonly IEnumerable<IPublisherBuilder> _publishers;
        private readonly IGameweekClient _gameweekClient;
        private readonly ILeagueClient _leagueClient;
        private readonly ITokenStore _tokenStore;
        private readonly IFetchFplbotSetup _setupFetcher;

        public FplStandingsCommandHandler(IEnumerable<IPublisherBuilder> publishers, IGameweekClient gameweekClient, ILeagueClient leagueClient, ITokenStore tokenStore, IFetchFplbotSetup setupFetcher)
        {
            _publishers = publishers;
            _gameweekClient = gameweekClient;
            _leagueClient = leagueClient;
            _tokenStore = tokenStore;
            _setupFetcher = setupFetcher;
        }

        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var standings = await GetStandings(message);

            foreach (var pBuilder in _publishers)
            {
                var p = await pBuilder.Build(message.Team.Id);
                await p.Publish(new Notification
                {
                    Recipient = message.ChatHub.Id,
                    Msg = standings
                });
            }

            return new HandleResponse(standings);
        }

        private async Task<string> GetStandings(SlackMessage message)
        {
            try
            {
                var token = await _tokenStore.GetTokenByTeamId(message.Team.Id);
                var setup = await _setupFetcher.GetSetupByToken(token);
                var leagueTask = _leagueClient.GetClassicLeague(setup.LeagueId);
                var gameweeksTask = _gameweekClient.GetGameweeks();
                var standings = Formatter.GetStandings(await leagueTask, await gameweeksTask);
                return standings;
            }
            catch (Exception e)
            {
                return $"Oops: {e.Message}";
            }
        }
        public bool ShouldHandle(SlackEvent slackEvent) => slackEvent is AppMentionEvent @event && @event.Text.Contains("standings");
        
        public (string,string) GetHelpDescription() => ("standings", "Get current league standings");
        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, SlackEvent slackEvent)
        {
            var rtmMessage = EventParser.ToBackCompatRtmMessage(eventMetadata, slackEvent);
            var messageHandled = await Handle(rtmMessage);     
            return new EventHandledResponse(messageHandled.HandledMessage);
        }
    }
}