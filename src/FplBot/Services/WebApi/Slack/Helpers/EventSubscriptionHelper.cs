namespace FplBot.Services.WebApi.Slack.Helpers;

public static class EventSubscriptionHelper
{
    public static IEnumerable<EventSubscription> GetAllSubscriptionTypes()
    {
        return Enum.GetValues(typeof(EventSubscription)).Cast<EventSubscription>();
    }
}
