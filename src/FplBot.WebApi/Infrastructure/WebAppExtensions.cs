using Discord.Net.Endpoints.Hosting;
using Serilog;
using Slackbot.Net.Endpoints.Hosting;

namespace FplBot.WebApi.Infrastructure;

public static class WebAppExtensions
{
    public static void UseWebApp(this WebApplication app)
    {
        var env = app.Environment;
        app.UseSerilogRequestLogging();
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseForwardedHeaders();
        if(!env.IsDevelopment())
            app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors(CorsOriginValidator.CustomCorsPolicyName);
        app.UseCookiePolicy();
        app.UseAuthentication();
        app.UseAuthorization();
        app.Map("/oauth/authorize", a => a.UseSlackbotDistribution());
        app.Map("/events", a => a.UseSlackbot(enableAuth: !env.IsDevelopment()));
        app.Map("/oauth/discord/authorize", a => a.UseDiscordDistribution());
        app.Map("/discord/events", a => a.UseDiscordbot(enableAuth: !env.IsDevelopment()));
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers().RequireCors(CorsOriginValidator.CustomCorsPolicyName);
            endpoints.MapRazorPages();
        });
    }

}
