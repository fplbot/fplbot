using Fpl.PulseLive;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.UnitTests;

public class PulseLiveClientRegistrationTests
{
    [Fact]
    public void RegisteringTwice_DoesNotDuplicateTheReferHeader()
    {
        // Reproduces running EventPublishers and WebApi in the same process (the local dev default,
        // no --services flag): both call AddPulseLiveClient, and without the dedup guard,
        // AddHttpClient stacks a second ConfigureHttpClient onto the same named client, which adds
        // "Referer" twice and throws since it only allows a single value.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPulseLiveClient();
        services.AddPulseLiveClient();

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IPulseLiveClient>();

        Assert.NotNull(client);
    }
}
