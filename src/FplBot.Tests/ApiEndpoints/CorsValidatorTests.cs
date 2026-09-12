using FplBot.WebApi.Infrastructure;

namespace FplBot.Tests.ApiEndpoints;

public class CorsValidatorTests
{
    [Theory]
    [InlineData("http://localhost:5162", true)]
    [InlineData("https://localhost:1337", true)]
    [InlineData("https://www.fplbot.app", true)]
    [InlineData("https://test.fplbot.app", true)]
    [InlineData("https://evil.example.com", false)]
    [InlineData("http://www.fplbot.app", false)] // wrong scheme
    [InlineData("https://fplbot.app", true)] // bare apex domain, no www
    public void ValidatesOrigins(string origin, bool expectedResult)
    {
        var isValid = CorsOriginValidator.ValidateOrigin(origin);
        Assert.Equal(expectedResult,isValid);
    }
}
