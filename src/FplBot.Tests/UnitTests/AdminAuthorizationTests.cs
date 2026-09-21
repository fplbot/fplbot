using System.Security.Claims;
using AspNet.Security.OAuth.Discord;
using AspNet.Security.OAuth.Slack;
using FplBot.WebApi.Configurations;
using FplBot.WebApi.Infrastructure;

namespace FplBot.Tests.UnitTests;

public class AdminAuthorizationTests
{
    private static ClaimsPrincipal SlackUser(string? teamId = "T1", string? userId = "U1", string? email = null) =>
        Principal(SlackAuthenticationDefaults.AuthenticationScheme,
            [
                new Claim(SlackAuthenticationConstants.Claims.TeamId, teamId ?? ""),
                new Claim(SlackAuthenticationConstants.Claims.UserId, userId ?? ""),
                .. email is null ? Array.Empty<Claim>() : [new Claim(ClaimTypes.Email, email)]
            ]);

    private static ClaimsPrincipal DiscordUser(string? email = "someone@example.test") =>
        Principal(DiscordAuthenticationDefaults.AuthenticationScheme,
            email is null ? [] : [new Claim(ClaimTypes.Email, email)]);

    private static ClaimsPrincipal Principal(string authenticationType, IEnumerable<Claim> claims) =>
        new(new ClaimsIdentity(claims, authenticationType));

    [Fact]
    public void SlackUser_NoAllowList_IsAuthorized()
    {
        Assert.True(AdminAuthorization.IsAuthorizedSlackUser(SlackUser(), new SlackAdminOptions()));
    }

    [Fact]
    public void SlackUser_MatchingTeamId_IsAuthorized()
    {
        var options = new SlackAdminOptions { AllowedTeamId = "T1" };
        Assert.True(AdminAuthorization.IsAuthorizedSlackUser(SlackUser(teamId: "T1"), options));
    }

    [Fact]
    public void SlackUser_WrongTeamId_IsNotAuthorized()
    {
        var options = new SlackAdminOptions { AllowedTeamId = "T1" };
        Assert.False(AdminAuthorization.IsAuthorizedSlackUser(SlackUser(teamId: "T2"), options));
    }

    [Fact]
    public void SlackUser_MatchingUserId_IsAuthorized()
    {
        var options = new SlackAdminOptions { AllowedUserIds = "U1,U2" };
        Assert.True(AdminAuthorization.IsAuthorizedSlackUser(SlackUser(userId: "U2"), options));
    }

    [Fact]
    public void SlackUser_UnlistedUserId_IsNotAuthorized()
    {
        var options = new SlackAdminOptions { AllowedUserIds = "U1,U2" };
        Assert.False(AdminAuthorization.IsAuthorizedSlackUser(SlackUser(userId: "U3"), options));
    }

    [Fact]
    public void SlackUser_MustMatchBothTeamAndUserIdWhenBothConfigured()
    {
        var options = new SlackAdminOptions { AllowedTeamId = "T1", AllowedUserIds = "U1" };

        Assert.True(AdminAuthorization.IsAuthorizedSlackUser(SlackUser(teamId: "T1", userId: "U1"), options));
        Assert.False(AdminAuthorization.IsAuthorizedSlackUser(SlackUser(teamId: "T1", userId: "U9"), options));
        Assert.False(AdminAuthorization.IsAuthorizedSlackUser(SlackUser(teamId: "T9", userId: "U1"), options));
    }

    [Fact]
    public void DiscordPrincipal_NeverSatisfiesTheSlackCheck()
    {
        Assert.False(AdminAuthorization.IsAuthorizedSlackUser(DiscordUser(), new SlackAdminOptions()));
    }

    [Fact]
    public void DiscordUser_NoAllowList_IsAuthorizedWhenPermittedByCaller()
    {
        Assert.True(AdminAuthorization.IsAuthorizedDiscordUser(DiscordUser(), new DiscordAdminOptions(), permitAnyWhenUnconfigured: true));
    }

    // Prod never gets a chance to set admin__AllowedEmails before the redirect URI goes live the
    // way Slack's AllowedTeamId/AllowedUserIds always have been — so unlike Slack, an empty list
    // must fail closed outside local dev, not wave every Discord account through.
    [Fact]
    public void DiscordUser_NoAllowList_IsNotAuthorizedWhenNotPermittedByCaller()
    {
        Assert.False(AdminAuthorization.IsAuthorizedDiscordUser(DiscordUser(), new DiscordAdminOptions(), permitAnyWhenUnconfigured: false));
    }

    [Fact]
    public void DiscordUser_MatchingEmail_IsAuthorized()
    {
        var options = new DiscordAdminOptions { AllowedEmails = "a@example.test,someone@example.test" };
        Assert.True(AdminAuthorization.IsAuthorizedDiscordUser(DiscordUser(email: "someone@example.test"), options, permitAnyWhenUnconfigured: false));
    }

    [Fact]
    public void DiscordUser_EmailMatchIsCaseInsensitive()
    {
        var options = new DiscordAdminOptions { AllowedEmails = "Someone@Example.Test" };
        Assert.True(AdminAuthorization.IsAuthorizedDiscordUser(DiscordUser(email: "someone@example.test"), options, permitAnyWhenUnconfigured: false));
    }

    [Fact]
    public void DiscordUser_UnlistedEmail_IsNotAuthorized()
    {
        var options = new DiscordAdminOptions { AllowedEmails = "a@example.test" };
        Assert.False(AdminAuthorization.IsAuthorizedDiscordUser(DiscordUser(email: "someone@example.test"), options, permitAnyWhenUnconfigured: false));
    }

    [Fact]
    public void DiscordUser_NoEmailClaim_IsNotAuthorizedWhenAllowListConfigured()
    {
        var options = new DiscordAdminOptions { AllowedEmails = "a@example.test" };
        Assert.False(AdminAuthorization.IsAuthorizedDiscordUser(DiscordUser(email: null), options, permitAnyWhenUnconfigured: false));
    }

    [Fact]
    public void SlackPrincipal_NeverSatisfiesTheDiscordCheck_EvenWithAMatchingEmailClaim()
    {
        var options = new DiscordAdminOptions { AllowedEmails = "someone@example.test" };
        Assert.False(AdminAuthorization.IsAuthorizedDiscordUser(SlackUser(email: "someone@example.test"), options, permitAnyWhenUnconfigured: true));
    }
}
