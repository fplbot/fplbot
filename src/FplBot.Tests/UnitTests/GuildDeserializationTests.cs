using System.Text.Json;
using Discord.Net.HttpClients;

namespace FplBot.Tests.UnitTests;

public class GuildDeserializationTests
{
    // Mirrors DiscordClient.SerializerOptions closely enough for this purpose: its Lowercase
    // policy only lowercases whole property names (no snake_case splitting), so for the
    // single-word "Id" field the web defaults' camelCase policy produces the same "id" wire
    // name; approximate_member_count is unaffected by either policy since JsonPropertyName wins.
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Deserializes_ApproximateMemberCount_FromSnakeCaseField()
    {
        var guild = JsonSerializer.Deserialize<Guild>(
            """{"id":"123","approximate_member_count":42}""", Options);

        Assert.NotNull(guild);
        Assert.Equal("123", guild.Id);
        Assert.Equal(42, guild.ApproximateMemberCount);
    }

    [Fact]
    public void Deserializes_WithoutApproximateMemberCountField_DefaultsToZero()
    {
        var guild = JsonSerializer.Deserialize<Guild>("""{"id":"123"}""", Options);

        Assert.NotNull(guild);
        Assert.Equal(0, guild.ApproximateMemberCount);
    }
}
