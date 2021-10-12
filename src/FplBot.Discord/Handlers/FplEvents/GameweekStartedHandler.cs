using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using FplBot.Discord.Data;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace FplBot.Discord.Handlers.FplEvents
{
    public class GameweekStartedHandler : IHandleMessages<GameweekJustBegan>, IHandleMessages<ProcessGameweekStartedForGuildChannel>
    {
        private readonly IGuildRepository _repo;
        private readonly ILeagueClient _leagueClient;
        private readonly ILogger<GameweekStartedHandler> _logger;
        private readonly ICaptainsByGameWeek _captainsByGameweek;
        private readonly ITransfersByGameWeek _transfersByGameweek;

        public GameweekStartedHandler(IGuildRepository repo, ILeagueClient leagueClient, ICaptainsByGameWeek captainsByGameweek, ITransfersByGameWeek transfersByGameweek, ILogger<GameweekStartedHandler> logger)
        {
            _repo = repo;
            _leagueClient = leagueClient;
            _logger = logger;
            _captainsByGameweek = captainsByGameweek;
            _transfersByGameweek = transfersByGameweek;
        }

        public async Task Handle(GameweekJustBegan notification, IMessageHandlerContext context)
        {
            var subs = await _repo.GetAllGuildSubscriptions();
            foreach (var team in subs)
            {
                await context.SendLocal(new ProcessGameweekStartedForGuildChannel(team.GuildId, team.ChannelId, notification.NewGameweek.Id));
            }
        }

        public async Task Handle(ProcessGameweekStartedForGuildChannel message, IMessageHandlerContext context)
        {
            var newGameweek = message.GameweekId;

            var team = await _repo.GetGuildSubscription(message.GuildId, message.ChannelId);

            var messages = new List<RichMesssage>();

            if (team.Subscriptions.ContainsSubscriptionFor(EventSubscription.Captains) ||
                team.Subscriptions.ContainsSubscriptionFor(EventSubscription.Transfers))
                messages.Add(new RichMesssage($"Gameweek {message.GameweekId}!", ""));


            var leagueExists = false;
            if (team.LeagueId.HasValue)
            {
                var league = await _leagueClient.GetClassicLeague(team.LeagueId.Value, tolerate404:true);
                leagueExists = league != null;
            }

            if (leagueExists && team.Subscriptions.ContainsSubscriptionFor(EventSubscription.Captains))
            {
                string captainsByGameWeek = await _captainsByGameweek.GetCaptainsByGameWeek(newGameweek, team.LeagueId.Value, includeExternalLinks:false);
                messages.Add(new RichMesssage("Captains:", captainsByGameWeek));
                string captainsChartByGameWeek = await _captainsByGameweek.GetCaptainsChartByGameWeek(newGameweek, team.LeagueId.Value);
                messages.Add(new RichMesssage("Chart", captainsChartByGameWeek));
            }
            else if (team.LeagueId.HasValue && !leagueExists && team.Subscriptions.ContainsSubscriptionFor(EventSubscription.Captains))
            {
                messages.Add(new RichMesssage("⚠️Warning!",$"️ You're subscribing to captains notifications, but following a league ({team.LeagueId.Value}) that does not exist. Update to a valid classic league, or unsubscribe to captains to avoid this message in the future."));
            }
            else
            {
                _logger.LogInformation("Team {team} hasn't subscribed for gw start captains, so bypassing it", team.GuildId);
            }

            if (leagueExists && team.Subscriptions.ContainsSubscriptionFor(EventSubscription.Transfers))
            {
                string transfersByGameweekTexts = await _transfersByGameweek.GetTransfersByGameweekTexts(newGameweek, team.LeagueId.Value, includeExternalLinks:false);
                messages.Add(new RichMesssage("Transfers", transfersByGameweekTexts));
            }
            else if (team.LeagueId.HasValue && !leagueExists && team.Subscriptions.ContainsSubscriptionFor(EventSubscription.Transfers))
            {
                messages.Add(new RichMesssage("⚠️Warning!", $"⚠️ You're subscribing to transfers notifications, but following a league ({team.LeagueId.Value}) that does not exist. Update to a valid classic league, or unsubscribe to transfers to avoid this message in the future."));
            }
            else
            {
                _logger.LogInformation("Team {team} hasn't subscribed for gw start transfers, so bypassing it", team.GuildId);
            }

            var i = 0;
            foreach (var richMessage in messages)
            {
                i = i + 2;
                var sendOptions = new SendOptions();
                sendOptions.DelayDeliveryWith(TimeSpan.FromSeconds(i));
                sendOptions.RouteToThisEndpoint();
                await context.Send(new PublishRichToGuildChannel(team.GuildId, team.ChannelId, richMessage.Title, richMessage.Description), sendOptions);
            }
        }
    }

    public record RichMesssage(string Title, string Description);
}
