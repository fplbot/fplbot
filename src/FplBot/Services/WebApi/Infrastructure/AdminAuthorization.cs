using System.Security.Claims;
using AspNet.Security.OAuth.Discord;
using AspNet.Security.OAuth.Slack;
using FplBot.WebApi.Configurations;

namespace FplBot.WebApi.Infrastructure;

public static class AdminAuthorization
{
    public static bool IsAuthorizedSlackUser(ClaimsPrincipal user, SlackAdminOptions options)
    {
        if (user.Identity?.AuthenticationType != SlackAuthenticationDefaults.AuthenticationScheme)
            return false;

        if (!string.IsNullOrEmpty(options.AllowedTeamId) &&
            user.FindFirst(SlackAuthenticationConstants.Claims.TeamId)?.Value != options.AllowedTeamId)
            return false;

        var allowedUserIds = SplitAllowList(options.AllowedUserIds);
        if (allowedUserIds.Length > 0)
        {
            var userId = user.FindFirst(SlackAuthenticationConstants.Claims.UserId)?.Value;
            if (userId is null || !allowedUserIds.Contains(userId))
                return false;
        }

        return true;
    }

    // permitAnyWhenUnconfigured mirrors the dev-permissive default AllowedTeamId/AllowedUserIds
    // already have for Slack — but unlike Slack's vars, admin__AllowedEmails has no chance to be
    // set from day one in prod, so an empty list there must fail closed rather than wave everyone
    // through. Callers pass env.IsLocal() so only Development/Integration get the permissive path.
    public static bool IsAuthorizedDiscordUser(ClaimsPrincipal user, DiscordAdminOptions options, bool permitAnyWhenUnconfigured)
    {
        if (user.Identity?.AuthenticationType != DiscordAuthenticationDefaults.AuthenticationScheme)
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
