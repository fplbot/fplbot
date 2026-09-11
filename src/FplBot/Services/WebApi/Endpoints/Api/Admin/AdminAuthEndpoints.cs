using AspNet.Security.OAuth.Slack;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

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

    private static IResult Login(string? returnUrl) =>
        TypedResults.Challenge(
            new AuthenticationProperties { RedirectUri = string.IsNullOrEmpty(returnUrl) ? "/admin" : returnUrl },
            authenticationSchemes: [SlackAuthenticationDefaults.AuthenticationScheme]);

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
