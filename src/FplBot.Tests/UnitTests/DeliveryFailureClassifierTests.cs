using System.Net;
using Discord.Net.HttpClients;
using FplBot.EventHandlers;

namespace FplBot.Tests.UnitTests;

public class DeliveryFailureClassifierTests
{
    [Theory]
    [InlineData("channel_not_found")]
    [InlineData("is_archived")]
    [InlineData("not_in_channel")]
    public void SlackChannelScopedErrors_Count(string error)
    {
        Assert.Equal(error, DeliveryFailureClassifier.Classify(error));
    }

    [Theory]
    [InlineData("account_inactive")]
    [InlineData("token_revoked")]
    [InlineData("invalid_auth")]
    [InlineData("msg_too_long")]
    [InlineData("no_text")]
    [InlineData("ratelimited")]
    [InlineData("some_error_nobody_has_seen")]
    [InlineData("")]
    public void SlackNonChannelScopedErrors_DoNotCount(string error)
    {
        Assert.Null(DeliveryFailureClassifier.Classify(error));
    }

    [Theory]
    [InlineData(10003, HttpStatusCode.NotFound)]
    [InlineData(50001, HttpStatusCode.Forbidden)]
    [InlineData(50013, HttpStatusCode.Forbidden)]
    public void DiscordChannelScopedErrors_Count(int code, HttpStatusCode status)
    {
        var result = DeliveryFailureClassifier.Classify(new DiscordApiException(status, code, "nope"));

        Assert.Equal(code.ToString(), result);
    }

    [Theory]
    [InlineData(10004, HttpStatusCode.NotFound)]
    [InlineData(50035, HttpStatusCode.BadRequest)]
    public void DiscordNonChannelScopedErrors_DoNotCount(int code, HttpStatusCode status)
    {
        Assert.Null(DeliveryFailureClassifier.Classify(new DiscordApiException(status, code, "nope")));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public void DiscordTransientAndAuthStatuses_DoNotCount(HttpStatusCode status)
    {
        Assert.Null(DeliveryFailureClassifier.Classify(new DiscordApiException(status, null, "nope")));
    }

    [Fact]
    public void DiscordUnknownCode_DoesNotCount()
    {
        Assert.Null(DeliveryFailureClassifier.Classify(new DiscordApiException(HttpStatusCode.Forbidden, 99999, "nope")));
    }

    [Fact]
    public void DiscordForbiddenWithNoCode_DoesNotCount()
    {
        Assert.Null(DeliveryFailureClassifier.Classify(new DiscordApiException(HttpStatusCode.Forbidden, null, "nope")));
    }
}
