using System.Security.Claims;
using AspNet.Security.OAuth.Discord;
using AspNet.Security.OAuth.Slack;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace FplBot.WebApi.Endpoints.Api.Admin;

public static class AdminAuthEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/login", Login).AllowAnonymous();
        group.MapPost("/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync();
            return TypedResults.NoContent();
        });
        group.MapGet("/me", Me).RequireAuthorization();
    }

    private static IResult Login(string? returnUrl, string? provider) =>
        TypedResults.Challenge(
            new AuthenticationProperties { RedirectUri = string.IsNullOrEmpty(returnUrl) ? "/admin" : returnUrl },
            authenticationSchemes: [ResolveScheme(provider)]);

    private static string ResolveScheme(string? provider) =>
        string.Equals(provider, "discord", StringComparison.OrdinalIgnoreCase)
            ? DiscordAuthenticationDefaults.AuthenticationScheme
            : SlackAuthenticationDefaults.AuthenticationScheme;

    private static async Task<IResult> Me(HttpContext httpContext, IAuthorizationService authorizationService)
    {
        var user = httpContext.User;
        var isAdmin = (await authorizationService.AuthorizeAsync(user, "IsAdmin")).Succeeded;

        return TypedResults.Ok(new
        {
            name = user.Identity?.Name,
            email = user.FindFirst(ClaimTypes.Email)?.Value,
            provider = user.Identity?.AuthenticationType,
            teamId = user.FindFirst("urn:slack:team_id")?.Value,
            teamName = user.FindFirst("urn:slack:team_name")?.Value,
            userId = user.FindFirst("urn:slack:user_id")?.Value,
            isAdmin
        });
    }
}
