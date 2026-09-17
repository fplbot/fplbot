using FplBot.EventHandlers.Discord.Helpers;

namespace FplBot.ApplicationServices.Slack;

public record CommandHelp(string Trigger, string Description);

public static class SlackCommandCatalog
{
    public static readonly CommandHelp Player = new("player {name}", "Display info about the player");
    public static readonly CommandHelp Standings = new("standings", "Get current league standings");
    public static readonly CommandHelp NextGameweek = new("next", "Displays the fixtures for next gameweek");
    public static readonly CommandHelp Injuries = new("injuries", "See injured players owned by more than 5 %");
    public static readonly CommandHelp Captains = new("captains [chart] {GW-number, or empty for current}", "Display captain picks in the league. Add \"chart\" to visualize it in a chart.");
    public static readonly CommandHelp Transfers = new("transfers {GW-number, or empty for current}", "Displays each team's transfers");
    public static readonly CommandHelp PriceChanges = new("pricechanges", "Displays players with recent price change");
    public static readonly CommandHelp Follow = new("follow {new league id}", "Set league to follow");
    public static readonly CommandHelp Subscribe = new("subscribe/unsubscribe {comma separated list of events}", $"Update what notifications fplbot should post. ({string.Join(", ", EventSubscriptionHelper.GetAllSubscriptionTypes())})");
    public static readonly CommandHelp Subscriptions = new("subscriptions", "List current subscriptions");
    public static readonly CommandHelp Search = new("search {name}", "(:wrench: Beta) Search for teams or leagues. E.g. \"search magnus carlsen\".");

    public static readonly IReadOnlyList<CommandHelp> All =
    [
        Player,
        Standings,
        NextGameweek,
        Injuries,
        Captains,
        Transfers,
        PriceChanges,
        Follow,
        Subscribe,
        Subscriptions,
        Search
    ];
}
