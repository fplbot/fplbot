using Fpl.Client.Abstractions;
using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class DiscordGameweekFinishedHandler(
    IGuildRepository repo,
    ILogger<DiscordGameweekFinishedHandler> logger,
    IGlobalSettingsClient settingsClient,
    ILeagueClient leagueClient)
    : IConsumer<GameweekFinished>,
        IConsumer<PublishGameweekFinishedToGuild>,
        IConsumer<PublishStandingsToDiscordGuild>
{
    public async Task Consume(ConsumeContext<GameweekFinished> context)
    {
        var message = context.Message;
        logger.LogInformation($"Gameweek {message.FinishedGameweek.Id} finished");
        var subscribedChannels = await repo.GetChannelsSubscribedTo(FplEvent.Standings);
        foreach (var (guildId, channelId) in subscribedChannels)
        {
            var channel = await repo.GetChannelSubscription(guildId, channelId);
            var leagueId = channel?.FollowedLeagueId is { } id ? (int)id.Value : (int?)null;
            if (leagueId is null)
            {
                continue;
            }
            await context.Publish(new PublishGameweekFinishedToGuild(guildId, channelId, leagueId, message.FinishedGameweek.Id));
        }
    }

    public async Task Consume(ConsumeContext<PublishGameweekFinishedToGuild> context)
    {
        var message = context.Message;
        var installation = await repo.FindInstallationByTeamId(message.GuildId);
        var sub = installation?.GetChannel(message.ChannelId);

        if (sub != null && message.LeagueId.HasValue && sub.IsSubscribedTo(FplEvent.Standings))
        {
            await PublishStandings(context, message.GuildId, message.ChannelId, message.LeagueId.Value, message.GameweekId);
        }
    }

    public async Task Consume(ConsumeContext<PublishStandingsToDiscordGuild> context)
    {
        var message = context.Message;
        await PublishStandings(context, message.GuildId, message.ChannelId, message.LeagueId, message.GameweekId);
    }

    private async Task PublishStandings(ConsumeContext context, string guildId, string channelId, int leagueId, int gameweekId)
    {
        var settings = await settingsClient.GetGlobalSettings();
        var gameweeks = settings?.Gameweeks ?? [];
        var gw = gameweeks.SingleOrDefault(g => g.Id == gameweekId);
        var league = await leagueClient.GetClassicLeague(leagueId, tolerate404:true);
        if (league != null && gw != null)
        {
            if (league.Properties?.StartEvent is var startEvent && gameweekId >= startEvent)
            {
                var sections = new List<RichSection>();
                var intro = Formatter.FormatGameweekFinished(gw, league, includeTitle:false);
                var standings = Formatter.GetStandingsDiscord(league, gw, includeExternalLinks:false);
                var topThree = Formatter.GetTopThreeGameweekEntries(league, gw, includeExternalLinks:false, includeIntro:false);
                var worst = league.Standings?.HasNext == true ? null : Formatter.GetWorstGameweekEntry(league, gw, includeExternalLinks:false);
                sections.AddRange([
                    new (null, intro),
                    new ($"{gw.Name}", topThree ?? string.Empty),
                    new ("Standings", standings)
                ]);

                if (worst is not null)
                {
                    sections.Add(new("ℹ️ Lantern beige", worst));
                }

                await context.Publish(new PublishSectionsToGuildChannel(guildId, channelId, "ℹ️ Gameweek finished!", sections));
            }
        }
        else
        {
            var msg = $"Standings are now generally ready, but you're subscribing to a non-classic or " +
                      $"non-existing classic FPL league: '{leagueId}'";
            await context.Publish(new PublishRichToGuildChannel(guildId, channelId, "⚠️ Standings ready", msg));
        }
    }
}
