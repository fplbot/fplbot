using System.Net;
using System.Text;
using Discord.Net.HttpClients;
using Discord.Net.HttpClients.Components;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace FplBot.Tests.UnitTests;

public class DiscordApiExceptionTests
{
    [Fact]
    public async Task Forbidden_WithMissingAccessBody_ThrowsWithErrorCode()
    {
        var client = BuildClient(HttpStatusCode.Forbidden, """{"message": "Missing Access", "code": 50001}""");

        var ex = await Assert.ThrowsAsync<DiscordApiException>(() => client.ChannelMessagePost("C1", "hi"));

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
        Assert.Equal(50001, ex.ErrorCode);
    }

    [Fact]
    public async Task NotFound_WithUnknownChannelBody_ThrowsWithErrorCode()
    {
        var client = BuildClient(HttpStatusCode.NotFound, """{"message": "Unknown Channel", "code": 10003}""");

        var ex = await Assert.ThrowsAsync<DiscordApiException>(() =>
            client.ChannelMessagePost("C1", new ComponentRequest([new TextDisplay("d")])));

        Assert.Equal(10003, ex.ErrorCode);
    }

    [Fact]
    public async Task Failure_WithUnparseableBody_ThrowsWithNullErrorCode()
    {
        var client = BuildClient(HttpStatusCode.InternalServerError, "<html>nope</html>");

        var ex = await Assert.ThrowsAsync<DiscordApiException>(() => client.ChannelMessagePost("C1", "hi"));

        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.Null(ex.ErrorCode);
    }

    [Fact]
    public async Task Failure_WithNullBody_ThrowsWithNullErrorCode()
    {
        var client = BuildClient(HttpStatusCode.InternalServerError, "null");

        var ex = await Assert.ThrowsAsync<DiscordApiException>(() => client.ChannelMessagePost("C1", "hi"));

        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.Null(ex.ErrorCode);
    }

    [Fact]
    public async Task Failure_WithArrayBody_ThrowsWithNullErrorCode()
    {
        var client = BuildClient(HttpStatusCode.InternalServerError, "[]");

        var ex = await Assert.ThrowsAsync<DiscordApiException>(() => client.ChannelMessagePost("C1", "hi"));

        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.Null(ex.ErrorCode);
    }

    [Fact]
    public async Task Failure_WithNonStringMessageBody_StillReadsErrorCode()
    {
        var client = BuildClient(HttpStatusCode.Forbidden, """{"message": 123, "code": 50001}""");

        var ex = await Assert.ThrowsAsync<DiscordApiException>(() => client.ChannelMessagePost("C1", "hi"));

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
        Assert.Equal(50001, ex.ErrorCode);
    }

    [Fact]
    public async Task Failure_WithNonNumericCodeBody_StillPreservesMessage()
    {
        var client = BuildClient(HttpStatusCode.Forbidden, """{"message": "Missing Access", "code": "abc"}""");

        var ex = await Assert.ThrowsAsync<DiscordApiException>(() => client.ChannelMessagePost("C1", "hi"));

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
        Assert.Null(ex.ErrorCode);
        Assert.Contains("Missing Access", ex.Message);
    }

    [Fact]
    public async Task Failure_WithEmptyBody_ThrowsWithNullErrorCode()
    {
        var client = BuildClient(HttpStatusCode.InternalServerError, "");

        var ex = await Assert.ThrowsAsync<DiscordApiException>(() => client.ChannelMessagePost("C1", "hi"));

        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.Null(ex.ErrorCode);
    }

    [Fact]
    public async Task IsCatchableAsHttpRequestException()
    {
        var client = BuildClient(HttpStatusCode.NotFound, """{"message": "Unknown Channel", "code": 10003}""");

        var caught = false;
        try
        {
            await client.ChannelMessagePost("C1", "hi");
        }
        catch (HttpRequestException hre) when (hre.StatusCode == HttpStatusCode.NotFound)
        {
            caught = true;
        }

        Assert.True(caught);
    }

    [Fact]
    public async Task Success_DoesNotThrow()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"id": "1"}""");

        await client.ChannelMessagePost("C1", "hi");
    }

    private static DiscordClient BuildClient(HttpStatusCode statusCode, string body)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(statusCode, body)) { BaseAddress = new Uri("https://discord.example/") };
        return new DiscordClient(httpClient,
            Options.Create(new DiscordClientOptions { DiscordApplicationId = "test", DiscordAppToken = "test" }),
            NullLogger<DiscordClient>.Instance);
    }

    private class StubHttpMessageHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
    }
}
