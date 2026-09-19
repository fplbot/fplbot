using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class DiscordGameweekStartedHandler(
    IGuildRepository repo,
    ILeagueClient leagueClient,
    ICaptainsByGameWeek captainsByGameweek,
    ITransfersByGameWeek transfersByGameweek,
    ILogger<DiscordGameweekStartedHandler> logger)
    : IConsumer<GameweekJustBegan>, IConsumer<ProcessGameweekStartedForGuildChannel>
{
    private const int MemberCountForLargeLeague = 25;

    public async Task Consume(ConsumeContext<GameweekJustBegan> context)
    {
        var notification = context.Message;
        var subscribedChannels = await repo.GetChannelsSubscribedTo(FplEvent.Captains, FplEvent.Transfers);
        foreach (var (guildId, channelId) in subscribedChannels)
        {
            await context.Publish(new ProcessGameweekStartedForGuildChannel(guildId, channelId, notification.NewGameweek.Id));
        }
    }

    public async Task Consume(ConsumeContext<ProcessGameweekStartedForGuildChannel> context)
    {
        var message = context.Message;
        var newGameweek = message.GameweekId;

        var installation = await repo.FindInstallationByTeamId(message.TeamId);
        var team = installation?.GetChannel(message.ChannelId);
        if (team is null)
        {
            logger.LogWarning("No subscription found for guild {GuildId} channel {ChannelId}. Skipping gameweek-started notifications", message.TeamId,
                message.ChannelId);
            return;
        }

        var leagueId = team.FollowedLeagueId is { } id ? (int)id.Value : (int?)null;
        var messages = new List<RichMesssage>();

        ClassicLeague? league = null;
        if (leagueId.HasValue)
        {
            league = await leagueClient.GetClassicLeague(leagueId.Value, tolerate404: true);
        }

        var leagueExists = league != null;
        var leagueStarted = league?.Properties?.StartEvent is var startEvent && newGameweek >= startEvent;

        if (leagueExists && leagueStarted && (team.IsSubscribedTo(FplEvent.Captains) ||
                                              team.IsSubscribedTo(FplEvent.Transfers)))
            messages.Add(new RichMesssage($"Gameweek {message.GameweekId}!", ""));

        if (leagueExists && leagueStarted && team.IsSubscribedTo(FplEvent.Captains))
        {
            var captainPicks = await captainsByGameweek.GetEntryCaptainPicks(newGameweek, leagueId!.Value);
            if (league!.Standings?.Entries.Count < MemberCountForLargeLeague)
            {
                var captainsByGameWeek = captainsByGameweek.GetCaptainsByGameWeek(newGameweek, captainPicks, includeExternalLinks: false);
                messages.Add(new RichMesssage("Captains:", captainsByGameWeek));
                var captainsChartByGameWeek = captainsByGameweek.GetCaptainsChartByGameWeek(newGameweek, captainPicks);
                messages.Add(new RichMesssage("Chart", captainsChartByGameWeek));
            }
            else
            {
                var captainsByGameWeek = captainsByGameweek.GetCaptainsStatsByGameWeek(captainPicks, includeHeader: false);
                messages.Add(new RichMesssage("Captain stats:", captainsByGameWeek));
            }
        }
        else if (leagueId.HasValue && !leagueExists && team.IsSubscribedTo(FplEvent.Captains))
        {
            messages.Add(new RichMesssage("⚠️Warning!",
                $"️ You're subscribing to captains notifications, but following a league ({leagueId.Value}) that does not exist. Update to a valid classic league, or unsubscribe to captains to avoid this message in the future."));
        }
        else
        {
            logger.LogInformation("Bypassing team {team} notifications. League started: {leagueStarted}", installation!.ExternalId, leagueStarted);
        }

        if (leagueExists && leagueStarted && team.IsSubscribedTo(FplEvent.Transfers))
        {
            if (league!.Standings?.Entries.Count < MemberCountForLargeLeague)
            {
                var transfersByGameweekTexts = await transfersByGameweek.GetTransferMessages(newGameweek, leagueId!.Value, includeExternalLinks: false);
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
                var externalLink = $"See https://www.fplbot.app/leagues/{leagueId!.Value} for full details";
                messages.Add(new RichMesssage("Captains/Transfers/Chips", externalLink));
            }
        }
        else if (leagueId.HasValue && !leagueExists && team.IsSubscribedTo(FplEvent.Transfers))
        {
            messages.Add(new RichMesssage("⚠️Warning!",
                $"⚠️ You're subscribing to transfers notifications, but following a league ({leagueId.Value}) that does not exist. Update to a valid classic league, or unsubscribe to transfers to avoid this message in the future."));
        }
        else
        {
            logger.LogInformation("Bypassing team {team} notifications. League started: {leagueStarted}", installation!.ExternalId, leagueStarted);
        }

        foreach (var richMessage in messages)
        {
            await context.Publish(new PublishRichToGuildChannel(installation!.ExternalId, message.ChannelId, richMessage.Title, richMessage.Description));
        }
    }
}

public record RichMesssage(string Title, string Description);
