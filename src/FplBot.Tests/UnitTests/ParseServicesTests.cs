using FplBot.Hosting;

namespace FplBot.Tests.UnitTests;

public class ParseServicesTests
{
    [Fact]
    public void WithoutTheFlag_AllServicesRun()
    {
        Assert.Equal(Enum.GetValues<FplBotService>(), new string[0].ParseServices());
    }

    [Fact]
    public void WithOtherArgsButNoFlag_AllServicesRun()
    {
        Assert.Equal(Enum.GetValues<FplBotService>(), new[] { "--urls", "http://localhost:1337" }.ParseServices());
    }

    [Fact]
    public void AllRunsEveryService()
    {
        Assert.Equal(Enum.GetValues<FplBotService>(), new[] { "--services", "all" }.ParseServices());
    }

    [Fact]
    public void ASingleServiceRunsOnlyThatOne()
    {
        Assert.Equal([FplBotService.WebApi], new[] { "--services", "WebApi" }.ParseServices());
    }

    [Fact]
    public void ACommaSeparatedListRunsEachOfThem()
    {
        Assert.Equal(
            [FplBotService.WebApi, FplBotService.EventHandlers],
            new[] { "--services", "WebApi, EventHandlers" }.ParseServices());
    }

    [Fact]
    public void TheFlagWithoutAValueIsAnError()
    {
        Assert.Throws<InvalidOperationException>(() => new[] { "--services" }.ParseServices());
    }
}
