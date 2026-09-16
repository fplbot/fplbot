using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;
using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.Formatting;

namespace FplBot.Discord.Handlers.SlashCommands;

public class AddSubscriptionSlashCommandHandler(IGuildRepository repo, ChannelDeliveryProbe probe) : ISlashCommandHandler
{
    public string CommandName => "subscriptions";

    public string SubCommandName => "add";

    public async Task<SlashCommandResponse> Handle(SlashCommandContext context)
    {
        var newEventSub = Enum.Parse<EventSubscription>(context.CommandInput!.Value);
        var newFplEvent = ToFplEvent(newEventSub);

        var installation = await repo.GetInstallation(context.GuildId);
        var existingChannel = installation.GetChannel(context.ChannelId);

        if (existingChannel == null)
        {
            installation.Subscribe(context.ChannelId, [newFplEvent]);
            await repo.Save(installation);
            var created = installation.GetChannel(context.ChannelId)!;
            return await RespondWithProbe(context, $"Added new subscription! Subscriptions:\n{Formatter.BulletPoints(created.Events.Current.Select(ToEventSubscription))}");
        }

        if (existingChannel.IsSubscribedTo(newFplEvent))
        {
            return Respond("⚠️", $"Already subscribing to {context.CommandInput.Value}");
        }

        installation.Subscribe(context.ChannelId, [newFplEvent]);
        await repo.Save(installation);
        return await RespondWithProbe(context, $"Updated subscriptions:\n{Formatter.BulletPoints(existingChannel.Events.Current.Select(ToEventSubscription))}");
    }

    private async Task<SlashCommandResponse> RespondWithProbe(SlashCommandContext context, string description)
    {
        var result = await probe.Probe(context.GuildId, context.ChannelId,
            "✅ Subscriptions updated for this channel.");

        return result.Delivered
            ? Respond("✅ Success!", description)
            : Respond("⚠️ Saved, but I can't post here yet",
                $"Subscribed! {ChannelDeliveryProbe.Advice(result)}\n\n{description}");
    }

    private static FplEvent ToFplEvent(EventSubscription e) => Enum.Parse<FplEvent>(e.ToString());
    private static EventSubscription ToEventSubscription(FplEvent e) => Enum.Parse<EventSubscription>(e.ToString());

    private static ChannelMessageWithSourceEmbedResponse Respond(string title, string description)
    {
        return new ChannelMessageWithSourceEmbedResponse { Embeds = [new RichEmbed(title, description)] };
    }
}
