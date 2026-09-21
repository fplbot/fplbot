using FplBot.Hosting;
using FplBot.WebApi.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FplBot.WebApi.Infrastructure;

public class IsAdminRequirement : IAuthorizationRequirement;

public class IsAdminAuthorizationHandler(
    IOptions<SlackAdminOptions> slackOptions,
    IOptions<DiscordAdminOptions> discordOptions,
    IHostEnvironment env)
    : AuthorizationHandler<IsAdminRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, IsAdminRequirement requirement)
    {
        if (AdminAuthorization.IsAuthorizedSlackUser(context.User, slackOptions.Value) ||
            AdminAuthorization.IsAuthorizedDiscordUser(context.User, discordOptions.Value, env.IsLocal()))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
