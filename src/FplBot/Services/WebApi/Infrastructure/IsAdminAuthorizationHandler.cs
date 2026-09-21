using FplBot.WebApi.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FplBot.WebApi.Infrastructure;

public class IsAdminRequirement : IAuthorizationRequirement;

public class IsAdminAuthorizationHandler(
    IOptions<AdminAllowedEmails> options,
    IHostEnvironment env)
    : AuthorizationHandler<IsAdminRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, IsAdminRequirement requirement)
    {
        if (AdminAuthorization.IsAuthorizedUser(context.User, options.Value, env.IsLocal()))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
