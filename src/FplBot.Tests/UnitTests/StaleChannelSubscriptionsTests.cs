using FakeItEasy;
using FplBot.Data;
using FplBot.Domain;
using FplBot.EventHandlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FplBot.Tests.UnitTests;

public class StaleChannelSubscriptionsTests
{
    [Fact]
    public async Task ClearFailures_WhenSaveThrows_DoesNotPropagate()
    {
        var subscription = ChannelSubscription.Load(SubscriptionId.New(), "C1", null, [], failureCount: 1);
        var repository = A.Fake<IDomainRepository>();
        A.CallTo(() => repository.GetChannelSubscription("G1", "C1")).Returns(subscription);
        A.CallTo(() => repository.SaveChannelSubscription("G1", subscription)).Throws(new TimeoutException("redis down"));

        await StaleChannelSubscriptions.ClearFailures(repository, "G1", "C1", NullLogger.Instance);
    }
}
