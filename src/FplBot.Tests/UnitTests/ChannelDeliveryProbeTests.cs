using Discord.Net.HttpClients;
using FakeItEasy;
using FplBot.Data.Discord;
using FplBot.Discord;
using FplBot.Domain;
using Microsoft.Extensions.Logging;

namespace FplBot.Tests.UnitTests;

public class ChannelDeliveryProbeTests
{
    [Fact]
    public async Task Probe_WhenPostSucceedsButClearingFailuresThrows_StillReportsDelivered()
    {
        var subscription = ChannelSubscription.Load("C1", null, [], failureCount: 1);
        var discordClient = A.Fake<IDiscordClient>();
        var repository = A.Fake<IGuildRepository>();
        A.CallTo(() => repository.GetChannelSubscription("G1", "C1")).Returns(subscription);
        A.CallTo(() => repository.SaveChannelSubscription("G1", subscription)).Throws(new TimeoutException("redis down"));

        var probe = new ChannelDeliveryProbe(discordClient, repository, A.Fake<ILogger<ChannelDeliveryProbe>>());

        var result = await probe.Probe("G1", "C1", "ping");

        Assert.True(result.Delivered);
    }
}
