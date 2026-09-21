using System.Security.Claims;
using AspNet.Security.OAuth.Discord;
using AspNet.Security.OAuth.Slack;
using FplBot.WebApi.Configurations;
using FplBot.WebApi.Infrastructure;

namespace FplBot.Tests.UnitTests;

public class AdminAuthorizationTests
{
    private static ClaimsPrincipal SlackUser(string? email = "someone@example.test") =>
        Principal(SlackAuthenticationDefaults.AuthenticationScheme, email);

    private static ClaimsPrincipal DiscordUser(string? email = "someone@example.test") =>
        Principal(DiscordAuthenticationDefaults.AuthenticationScheme, email);

    private static ClaimsPrincipal Principal(string authenticationType, string? email) =>
        new(new ClaimsIdentity(
            email is null ? [] : [new Claim(ClaimTypes.Email, email)],
            authenticationType));

    [Fact]
    public void NoAllowList_IsAuthorizedWhenPermittedByCaller()
    {
        Assert.True(AdminAuthorization.IsAuthorizedUser(SlackUser(), new AdminAllowedEmails(), permitAnyWhenUnconfigured: true));
        Assert.True(AdminAuthorization.IsAuthorizedUser(DiscordUser(), new AdminAllowedEmails(), permitAnyWhenUnconfigured: true));
    }

    // Prod never gets a chance to set admin__AllowedEmails before the login providers go live —
    // unlike the old Slack-only AllowedTeamId/AllowedUserIds, which were always set in prod — so
    // an empty list must fail closed outside local dev, not wave everyone through.
    [Fact]
    public void NoAllowList_IsNotAuthorizedWhenNotPermittedByCaller()
    {
        Assert.False(AdminAuthorization.IsAuthorizedUser(SlackUser(), new AdminAllowedEmails(), permitAnyWhenUnconfigured: false));
        Assert.False(AdminAuthorization.IsAuthorizedUser(DiscordUser(), new AdminAllowedEmails(), permitAnyWhenUnconfigured: false));
    }

    [Fact]
    public void SlackUser_MatchingEmail_IsAuthorized()
    {
        var options = new AdminAllowedEmails { AllowedEmails = "a@example.test,someone@example.test" };
        Assert.True(AdminAuthorization.IsAuthorizedUser(SlackUser(email: "someone@example.test"), options, permitAnyWhenUnconfigured: false));
    }

    [Fact]
    public void DiscordUser_MatchingEmail_IsAuthorized()
    {
        var options = new AdminAllowedEmails { AllowedEmails = "a@example.test,someone@example.test" };
        Assert.True(AdminAuthorization.IsAuthorizedUser(DiscordUser(email: "someone@example.test"), options, permitAnyWhenUnconfigured: false));
    }

    [Fact]
    public void EmailMatchIsCaseInsensitive()
    {
        var options = new AdminAllowedEmails { AllowedEmails = "Someone@Example.Test" };
        Assert.True(AdminAuthorization.IsAuthorizedUser(DiscordUser(email: "someone@example.test"), options, permitAnyWhenUnconfigured: false));
    }

    [Fact]
    public void UnlistedEmail_IsNotAuthorized()
    {
        var options = new AdminAllowedEmails { AllowedEmails = "a@example.test" };
        Assert.False(AdminAuthorization.IsAuthorizedUser(SlackUser(email: "someone@example.test"), options, permitAnyWhenUnconfigured: false));
        Assert.False(AdminAuthorization.IsAuthorizedUser(DiscordUser(email: "someone@example.test"), options, permitAnyWhenUnconfigured: false));
    }

    [Fact]
    public void NoEmailClaim_IsNotAuthorizedWhenAllowListConfigured()
    {
        var options = new AdminAllowedEmails { AllowedEmails = "a@example.test" };
        Assert.False(AdminAuthorization.IsAuthorizedUser(SlackUser(email: null), options, permitAnyWhenUnconfigured: false));
        Assert.False(AdminAuthorization.IsAuthorizedUser(DiscordUser(email: null), options, permitAnyWhenUnconfigured: false));
    }

    [Fact]
    public void UnrecognizedAuthenticationScheme_IsNeverAuthorized_EvenWithAMatchingEmailClaim()
    {
        var options = new AdminAllowedEmails { AllowedEmails = "someone@example.test" };
        var user = Principal("SomeOtherScheme", "someone@example.test");
        Assert.False(AdminAuthorization.IsAuthorizedUser(user, options, permitAnyWhenUnconfigured: true));
    }
}
