using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data.Discord;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.EventHandlers.Discord;

public class GameweekStartedHandler : IHandleMessages<GameweekJustBegan>, IHandleMessages<ProcessGameweekStartedForGuildChannel>
{
    private const int MemberCountForLargeLeague = 25;
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
            var options = new SendOptions();
            options.RequireImmediateDispatch();
            options.RouteToThisEndpoint();
            await context.Send(new ProcessGameweekStartedForGuildChannel(team.GuildId, team.ChannelId, notification.NewGameweek.Id), options);
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



        ClassicLeague league = null;
        if (team.LeagueId.HasValue)
        {
            league = await _leagueClient.GetClassicLeague(team.LeagueId.Value, tolerate404:true);
        }

        var leagueExists = league != null;

        if (leagueExists && team.Subscriptions.ContainsSubscriptionFor(EventSubscription.Captains))
        {
            var captainPicks = await _captainsByGameweek.GetEntryCaptainPicks(newGameweek, team.LeagueId.Value);
            if (league.Standings.Entries.Count < MemberCountForLargeLeague)
            {
                string captainsByGameWeek = _captainsByGameweek.GetCaptainsByGameWeek(newGameweek, captainPicks, includeExternalLinks:false);
                messages.Add(new RichMesssage("Captains:", captainsByGameWeek));
                string captainsChartByGameWeek = _captainsByGameweek.GetCaptainsChartByGameWeek(newGameweek, captainPicks);
                messages.Add(new RichMesssage("Chart", captainsChartByGameWeek));
            }
            else
            {
                string captainsByGameWeek = _captainsByGameweek.GetCaptainsStatsByGameWeek(captainPicks, includeHeader:false);
                messages.Add(new RichMesssage("Captain stats:", captainsByGameWeek));
            }

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
            if (league.Standings.Entries.Count < MemberCountForLargeLeague)
            {
                var transfersByGameweekTexts = await _transfersByGameweek.GetTransferMessages(newGameweek, team.LeagueId.Value, includeExternalLinks:false);
                // Discord max limit is 2000 chars, so chunking by 4 managers
                if (transfersByGameweekTexts.GetTotalCharCount() > 2000)
                {
                    var array = transfersByGameweekTexts.Messages.Chunk(4).ToArray();

                    messages.Add(new RichMesssage("Transfers", string.Join("", array.First().Select(c => c.Message))));
                    foreach (var partial in array[1..])
                    {
                        messages.Add(new RichMesssage("", string.Join("", partial.Select(c => c.Message))));
                    }
                }
                else
                {
                    messages.Add(new RichMesssage("Transfers", string.Join("", transfersByGameweekTexts.Messages.Select(m => m.Message))));
                }
            }
            else
            {
                var externalLink = $"See https://www.fplbot.app/leagues/{team.LeagueId.Value} for full details";
                messages.Add(new RichMesssage("Captains/Transfers/Chips", externalLink));
            }
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
