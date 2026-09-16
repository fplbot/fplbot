using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;
using Fpl.Client.Abstractions;
using FplBot.Data.Discord;
using FplBot.Domain;

namespace FplBot.Discord.Handlers.SlashCommands;

public class FollowSlashCommandHandler(ILeagueClient leagueClient, IGuildRepository repo, ChannelDeliveryProbe probe) : ISlashCommandHandler
{
    public string CommandName => "follow";

    public async Task<SlashCommandResponse> Handle(SlashCommandContext context)
    {
        var leagueId = int.Parse(context.CommandInput!.Value);
        var league = await leagueClient.GetClassicLeague(leagueId, tolerate404:true);

        if(league == null)
            return Respond($"Could not find a classic league of id '{leagueId}'", success:false);

        var installation = await repo.GetInstallation(context.GuildId);
        var isNewChannel = installation.GetChannel(context.ChannelId) is null;

        installation.Follow(context.ChannelId, new ClassicLeagueId(leagueId));
        await repo.Save(installation);

        var leagueName = league.Properties?.Name;
        var result = await probe.Probe(context.GuildId, context.ChannelId,
            $"✅ Now following '{leagueName}' in this channel.");

        if (!result.Delivered)
        {
            return RespondSavedButBlocked(
                $"Following '{leagueName}'! But there is a permission issue you need to solve. {ChannelDeliveryProbe.ProblemAndFix(result)}");
        }

        return isNewChannel
            ? Respond($"Now following the '{leagueName}' FPL league. (Auto-subbed to all events) ")
            : Respond($"Now following the '{leagueName}' FPL league. ");
    }

    private static SlashCommandResponse Respond(string content, bool success = true)
    {
        return new ChannelMessageWithSourceEmbedResponse
               {
            Embeds = [success ? new("✅ Success", content) : new("⚠️ Error", content)]
        };
    }

    private static SlashCommandResponse RespondSavedButBlocked(string content) =>
        new ChannelMessageWithSourceEmbedResponse
        {
            Embeds = [new("⚠️ Saved, but I can't post here yet", content)]
        };
}
