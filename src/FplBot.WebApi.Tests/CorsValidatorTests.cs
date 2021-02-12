using Xunit;

namespace FplBot.WebApi.Tests
{
    public class CorsValidatorTests
    {
        [Theory]
        [InlineData("http://localhost:3000", true)]
        [InlineData("https://fplbot-frontend.herokuapp.com", true)]
        [InlineData("https://www.fplbot.app", true)]
        [InlineData("https://fplbotfrontend-pr-4.herokuapp.com", true)]
        [InlineData("https://fplbotfrontend-pr-12.herokuapp.com", true)]
        [InlineData("https://someotherdomain-pr-12.herokuapp.com", false)]
        [InlineData("https://www.fplsearch.com", true)]
        [InlineData("https://fplsearch.com", true)]
        public void ValidatesOrigins(string origin, bool expectedResult)
        {
            var isValid = CorsOriginValidator.ValidateOrigin(origin);
            Assert.Equal(expectedResult,isValid);
        }
    }
}