using System.Net;
using AspNet.Security.OAuth.Slack;
using FplBot.WebApi.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Extensions.Publishers.Logger;
using Slackbot.Net.Extensions.Publishers.Slack;
using Slackbot.Net.SlackClients.Http.Extensions;
using StackExchange.Redis;

namespace FplBot.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSlackbotOauthAccessHttpClient();
            services.Configure<RedisOptions>(Configuration);
            services.Configure<DistributedSlackAppOptions>(Configuration);
            services.AddSingleton<ConnectionMultiplexer>(c =>
            {
                var opts = c.GetService<IOptions<RedisOptions>>().Value;
                return ConnectionMultiplexer.Connect($"{opts.REDIS_SERVER}, name={opts.REDIS_USERNAME}, password={opts.REDIS_PASSWORD}");
            });
            services.AddSingleton<ISlackTeamRepository, RedisSlackTeamRepository>();
            services.AddSlackbotWorker<RedisSlackTeamRepository>()
                .AddSlackPublisherBuilder()
                .AddLoggerPublisherBuilder()
                .AddDistributedFplBot<RedisSlackTeamRepository>(Configuration.GetSection("fpl"))
                .BuildRecurrers();
            services.AddAuthentication(options =>
                {
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignOutScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                })
                .AddCookie(o =>
                {
                    o.Cookie.Name = "fplbot-admin";
                    o.AccessDeniedPath = "/forbidden";
                    o.ReturnUrlParameter = "r";
                    o.ForwardChallenge = SlackAuthenticationDefaults.AuthenticationScheme;
                })
                .AddSlack(c =>
                {
                    c.ClientId = Configuration.GetValue<string>("CLIENT_ID");
                    c.ClientSecret = Configuration.GetValue<string>("CLIENT_SECRET");
                });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("IsHeltBlankSlackUser", b => b.RequireClaim("urn:slack:team_id", "T0A9QSU83"));
            });
            services.AddRazorPages().AddRazorPagesOptions(options =>
            {
                
                options.Conventions.AuthorizeFolder("/admin", "IsHeltBlankSlackUser");
                options.Conventions.AllowAnonymousToPage("/*");
            });
            services.Configure<RouteOptions>(o =>
            {
                o.LowercaseQueryStrings = true;
                o.LowercaseUrls = true;
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
        }
    }

    public class HasBlankEmail : IAuthorizationRequirement
    {
    }
}
