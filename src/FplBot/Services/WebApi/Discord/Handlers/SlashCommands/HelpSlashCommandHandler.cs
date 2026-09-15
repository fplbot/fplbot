using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;
using Fpl.Client.Abstractions;
using FplBot.Data.Discord;
using FplBot.Discord.Extensions;
using FplBot.Domain;

namespace FplBot.Discord.Handlers.SlashCommands;

public class HelpSlashCommandHandler(IGuildRepository repo, ILeagueClient client) : ISlashCommandHandler
{
    public string CommandName => "help";

    public async Task<SlashCommandResponse> Handle(SlashCommandContext context)
    {
        var content = "";
        var installation = await repo.GetInstallation(context.GuildId);
        var sub = installation.GetChannel(context.ChannelId);
        if (sub != null)
        {
            if (sub.FollowedLeagueId is { } leagueId)
            {
                var league = await client.GetClassicLeague((int)leagueId.Value, tolerate404:true);
                if(league != null)
                    content += $"\n**League:**\nCurrently following the '{league.Properties?.Name}' league";
            }
            else
            {
                content += "\n ⚠️ Not following any FPL leagues";
            }

            var allTypes = EventSubscriptionHelper.GetAllSubscriptionTypes();
            var currentSubs = sub.Events.Current.Select(ToEventSubscription).ToList();
            if (currentSubs.Count != 0)
            {
                content += $"\n\n**Subscriptions:**\n{string.Join("\n", currentSubs.Select(s => $" ✅ {s}"))}";

                if (!currentSubs.Contains(EventSubscription.All))
                {
                    var allTypesExceptSubs = allTypes.Except(currentSubs).Except([EventSubscription.All]);
                    content += $"\n\n**Not subscribing:**\n{string.Join("\n", allTypesExceptSubs.Select(s => $" ❌ {s}"))}";
                }
            }
            else
            {
                content += "\n\n**Subscriptions:**\n ⚠️ No subscriptions";
                content += $"\n\n**Events you may subscribe to:**\n{string.Join("\n", allTypes.Select(s => $" ▪️ {s}"))}";
            }
        }
        else
        {
            content = "⚠️ Not subscribing to any events. Add one to get notifications!";
        }

        return Respond(content);
    }

    private static EventSubscription ToEventSubscription(FplEvent e) => Enum.Parse<EventSubscription>(e.ToString());

    private static ChannelMessageWithSourceEmbedResponse Respond(string content)
    {
        return new ChannelMessageWithSourceEmbedResponse { Embeds = [new RichEmbed("ℹ️ HELP", content)] };
    }
}
