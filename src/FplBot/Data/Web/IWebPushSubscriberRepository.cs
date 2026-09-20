using FplBot.Domain;

namespace FplBot.Data.Web;

public interface IWebPushSubscriberRepository
{
    Task<WebPushSubscriber?> Find(WebPushSubscriberId id);
    Task Save(WebPushSubscriber subscriber);
    Task Delete(WebPushSubscriberId id);
    Task<IEnumerable<WebPushSubscriberId>> GetSubscribedTo(params FplEvent[] fplEvents);
    Task<IEnumerable<(WebPushSubscriberId Id, ClassicLeagueId LeagueId)>> GetFollowingALeague(params FplEvent[] fplEvents);
    Task<(IReadOnlyList<WebPushSubscriber> Items, int TotalCount)> GetPage(int page, int pageSize);
}
