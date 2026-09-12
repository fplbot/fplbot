namespace FplBot.WebApi.Infrastructure;

public static class CorsOriginValidator
{
    public const string CorsPolicyName = "AllowedOriginsCorsPolicy";

    public static List<string> FixedOrigins =
    [
        "http://localhost:5162",

        "https://localhost:1337",

        "https://www.fplbot.app",

        "https://fplbot.app",

        "https://test.fplbot.app"
    ];

    public static bool ValidateOrigin(string origin) =>
        FixedOrigins.Any(o => o.Equals(origin, StringComparison.InvariantCultureIgnoreCase));
}
