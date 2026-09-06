using FplBot.Data.Discord;

namespace FplBot.Discord.Extensions;

public static class EventSubscriptionHelper
{
    public static IEnumerable<EventSubscription> GetAllSubscriptionTypes()
    {
        return Enum.GetValues(typeof(EventSubscription)).Cast<EventSubscription>();
    }
}
