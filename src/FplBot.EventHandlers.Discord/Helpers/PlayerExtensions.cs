using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.EventHandlers.Discord.Helpers;

public static class PlayerExtensions
{
    private const int THRESHOLD = 0;

    public static bool IsRelevant(this InjuredPlayer player)
    {
        return player.OwnershipPercentage > THRESHOLD;
    }

    public static bool IsRelevant(this PlayerWithPriceChange player)
    {
        return player.OwnershipPercentage > THRESHOLD;
    }
}
