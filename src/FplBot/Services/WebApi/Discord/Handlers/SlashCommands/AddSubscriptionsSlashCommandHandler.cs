using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;
using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.Formatting;

namespace FplBot.Discord.Handlers.SlashCommands;

public class AddSubscriptionSlashCommandHandler(IGuildRepository repo) : ISlashCommandHandler
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
            return Respond("✅ Success!", $"Added new subscription! Subscriptions:\n{Formatter.BulletPoints(created.Events.Current.Select(ToEventSubscription))}");
        }

        if (existingChannel.IsSubscribedTo(newFplEvent))
        {
            return Respond("⚠️", $"Already subscribing to {context.CommandInput.Value}");
        }

        installation.Subscribe(context.ChannelId, [newFplEvent]);
        await repo.Save(installation);
        return Respond("✅ Success!", $"Updated subscriptions:\n{Formatter.BulletPoints(existingChannel.Events.Current.Select(ToEventSubscription))}");
    }

    private static FplEvent ToFplEvent(EventSubscription e) => Enum.Parse<FplEvent>(e.ToString());
    private static EventSubscription ToEventSubscription(FplEvent e) => Enum.Parse<EventSubscription>(e.ToString());

    private static ChannelMessageWithSourceEmbedResponse Respond(string title, string description)
    {
        return new ChannelMessageWithSourceEmbedResponse { Embeds = [new RichEmbed(title, description)] };
    }
}
