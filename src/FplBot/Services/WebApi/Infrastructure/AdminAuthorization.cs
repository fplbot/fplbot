using System.Security.Claims;
using AspNet.Security.OAuth.Discord;
using AspNet.Security.OAuth.Slack;
using FplBot.WebApi.Configurations;

namespace FplBot.WebApi.Infrastructure;

public static class AdminAuthorization
{
    private static readonly string[] AdminLoginSchemes =
    [
        SlackAuthenticationDefaults.AuthenticationScheme,
        DiscordAuthenticationDefaults.AuthenticationScheme
    ];

    // Prod never gets a chance to set admin__AllowedEmails before the login providers go live
    // the way Slack's old AllowedTeamId/AllowedUserIds always did — so an empty list must fail
    // closed outside local dev, not wave everyone through. Callers pass env.IsLocal() so only
    // Development/Integration get the permissive path.
    public static bool IsAuthorizedUser(ClaimsPrincipal user, AdminAllowedEmails options, bool permitAnyWhenUnconfigured)
    {
        if (user.Identity?.AuthenticationType is not { } scheme || !AdminLoginSchemes.Contains(scheme))
            return false;

        var allowedEmails = SplitAllowList(options.AllowedEmails);
        if (allowedEmails.Length == 0)
            return permitAnyWhenUnconfigured;

        var email = user.FindFirst(ClaimTypes.Email)?.Value;
        return email is not null && allowedEmails.Contains(email, StringComparer.OrdinalIgnoreCase);
    }

    private static string[] SplitAllowList(string? value) =>
        (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
