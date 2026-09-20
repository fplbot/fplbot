using FplBot.Integrations.WebPush;

namespace FplBot.Tests.UnitTests;

public class WebPushPayloadJsonTests
{
    [Fact]
    public void Serialize_UsesLowercaseKeysMatchingTheServiceWorker()
    {
        var json = WebPushPayloadJson.Serialize(new WebPushPayload("Title text", "Body text", "/leagues/123"));

        Assert.Contains("\"title\":\"Title text\"", json);
        Assert.Contains("\"body\":\"Body text\"", json);
        Assert.Contains("\"link\":\"/leagues/123\"", json);
        Assert.DoesNotContain("\"Title\":", json);
        Assert.DoesNotContain("\"Body\":", json);
        Assert.DoesNotContain("\"Link\":", json);
    }

    [Fact]
    public void Serialize_WritesNullLinkRatherThanOmittingIt()
    {
        var json = WebPushPayloadJson.Serialize(new WebPushPayload("Title text", "Body text", null));

        Assert.Contains("\"link\":null", json);
    }
}
