using System;
using System.Threading.Tasks;
using AspNet.Security.OAuth.Slack;
using FplBot.WebApi.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Endpoints.Hosting;
using Slackbot.Net.Extensions.FplBot.Abstractions;
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
                var options = new ConfigurationOptions
                {
                    ClientName = opts.GetRedisUsername,
                    Password = opts.GetRedisPassword,
                    EndPoints = {opts.GetRedisServerHostAndPort}
                };
                return ConnectionMultiplexer.Connect(options);
            });
            services.AddSingleton<ISlackTeamRepository, RedisSlackTeamRepository>();
            services.Configure<AnalyticsOptions>(Configuration);
            services.AddDistributedFplBot<RedisSlackTeamRepository>(Configuration.GetSection("fpl"))
                .AddFplBotEventHandlers<RedisSlackTeamRepository>(c =>
                {
                    c.Client_Id = Configuration.GetValue<string>("CLIENT_ID");
                    c.Client_Secret = Configuration.GetValue<string>("CLIENT_SECRET");
                });

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
                    c.Scope.Add("identity.team");

                    c.Events.OnRemoteFailure = r =>
                    {
                        var errorMsg = r.Request.Query["error"];
                        r.Response.Redirect($"/error?msg={errorMsg}");
                        r.HandleResponse();
                        return Task.FromResult(0);
                    };
                });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("IsHeltBlankSlackUser", b => b.RequireClaim("urn:slack:team_id", "T0A9QSU83"));
            });
            services
                .AddRazorPages()
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AuthorizeFolder("/admin", "IsHeltBlankSlackUser");
                    options.Conventions.AllowAnonymousToPage("/*");
                })
                .AddRazorRuntimeCompilation();

            services.Configure<RouteOptions>(o =>
            {
                o.LowercaseQueryStrings = true;
                o.LowercaseUrls = true;
            });
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
            services.AddCors(options =>
            {
                options.AddPolicy(CorsOriginValidator.CustomCorsPolicyName, p => 
                    p.SetIsOriginAllowed(CorsOriginValidator.ValidateOrigin).AllowAnyHeader().AllowAnyMethod()
                );
            });
            services.AddHttpContextAccessor();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSerilogRequestLogging();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseForwardedHeaders();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors(CorsOriginValidator.CustomCorsPolicyName);
            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSlackbot("/events");
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers().RequireCors(CorsOriginValidator.CustomCorsPolicyName);
                endpoints.MapRazorPages();
            });
        }
    }
}
