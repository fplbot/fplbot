using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;
using Fpl.Client.Abstractions;
using FplBot.Data.Discord;

namespace FplBot.Discord.Handlers.SlashCommands;

public class FollowSlashCommandHandler(ILeagueClient leagueClient, IGuildRepository repo) : ISlashCommandHandler
{
    public string CommandName => "follow";

    public async Task<SlashCommandResponse> Handle(SlashCommandContext context)
    {
        var leagueId = int.Parse(context.CommandInput!.Value);
        var league = await leagueClient.GetClassicLeague(leagueId, tolerate404:true);

        if(league == null)
            return Respond($"Could not find a classic league of id '{leagueId}'", success:false);

        var existingSub = await repo.GetGuildSubscription(context.GuildId, context.ChannelId);
        if (existingSub == null)
        {
            await repo.InsertGuildSubscription(new GuildFplSubscription(context.GuildId, context.ChannelId, leagueId, new []
            {
                EventSubscription.All
            }));

            return Respond($"Now following the '{$"{league.Properties?.Name}"}' FPL league. (Auto-subbed to all events) ");
        }

        await repo.UpdateGuildSubscription(existingSub with { LeagueId = leagueId });
        return Respond($"Now following the '{$"{league.Properties?.Name}"}' FPL league. " );

    }

    private static SlashCommandResponse Respond(string content, bool success = true)
    {
        return new ChannelMessageWithSourceEmbedResponse()
        {
            Embeds = new List<RichEmbed>
            {
                success ? new("✅ Success", content) : new ("⚠️ Error", content)
            }
        };
    }
}
