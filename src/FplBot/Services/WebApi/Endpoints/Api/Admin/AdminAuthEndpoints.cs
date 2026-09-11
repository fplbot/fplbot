using AspNet.Security.OAuth.Slack;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace FplBot.WebApi.Endpoints.Api.Admin;

public static class AdminAuthEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/login", Login).AllowAnonymous();
        // Cast to Delegate: a handler whose only parameter is HttpContext is otherwise an
        // exact signature match for RequestDelegate (via Task<IResult>-to-Task covariance),
        // so MapPost would silently pick that overload and discard the returned IResult
        // instead of writing it to the response (ASP0016).
        group.MapPost("/logout", (Delegate)Logout).AllowAnonymous();
        group.MapGet("/me", Me).RequireAuthorization();
    }

    private static IResult Login(string? returnUrl) =>
        TypedResults.Challenge(
            new AuthenticationProperties { RedirectUri = string.IsNullOrEmpty(returnUrl) ? "/admin" : returnUrl },
            authenticationSchemes: [SlackAuthenticationDefaults.AuthenticationScheme]);

    private static async Task<IResult> Logout(HttpContext httpContext)
    {
        await httpContext.SignOutAsync();
        return TypedResults.NoContent();
    }

    private static async Task<IResult> Me(HttpContext httpContext, IAuthorizationService authorizationService)
    {
        var user = httpContext.User;
        var isAdmin = (await authorizationService.AuthorizeAsync(user, "IsAdmin")).Succeeded;

        return TypedResults.Ok(new
        {
            name = user.Identity?.Name,
            teamId = user.FindFirst("urn:slack:team_id")?.Value,
            teamName = user.FindFirst("urn:slack:team_name")?.Value,
            userId = user.FindFirst("urn:slack:user_id")?.Value,
            isAdmin
        });
    }
}
