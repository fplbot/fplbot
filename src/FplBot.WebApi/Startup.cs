using AspNet.Security.OAuth.Slack;
using FplBot.WebApi.Configurations;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Slackbot.Net.Endpoints.Hosting;
using Slackbot.Net.SlackClients.Http.Extensions;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MediatR;
using Slackbot.Net.Endpoints.Authentication;

namespace FplBot.WebApi
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            _env = env;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDataProtection()
                .SetApplicationName(
                    "fplbot"); // set static so cookies are not encrypted differently after a reboot/deploy. https://github.com/dotnet/aspnetcore/issues/2513#issuecomment-354683162
            services.AddControllers()
                .AddJsonOptions(opts =>
                {
                    opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            services.AddSlackbotOauthAccessHttpClient();
            services.Configure<OAuthOptions>(Configuration);
            services.Configure<AnalyticsOptions>(Configuration);
            services.AddReducedHttpClientFactoryLogging();
            services.AddFplBot(Configuration).AddFplBotSlackEventHandlers();
            services.AddFplWorkers();
            services.AddMediatR(typeof(FplEventHandlers));
            services.AddMediatR(typeof(Startup));
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
                })
                .AddSlackbotEvents(c =>
                {
                    c.SigningSecret = Configuration.GetValue<string>("CLIENT_SIGNING_SECRET");
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("IsAdmin", b =>
                {
                    b.RequireClaim("urn:slack:team_id", "T016B9N3U7P");
                    b.RequireClaim("urn:slack:user_id", "U016CP6EPR8", "U0172HKTB08", "U016CSWNXAP");
                });
            });
            var mvcBuilder = services
                .AddRazorPages()
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AuthorizeFolder("/admin", "IsAdmin");
                    options.Conventions.AllowAnonymousToPage("/*");
                });

            if (_env.IsDevelopment())
            {
                mvcBuilder.AddRazorRuntimeCompilation();
            }

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
            if(!env.IsDevelopment())
                app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors(CorsOriginValidator.CustomCorsPolicyName);
            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseAuthorization();
            app.Map("/events", a => a.UseSlackbot(enableAuth: !_env.IsDevelopment()));
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers().RequireCors(CorsOriginValidator.CustomCorsPolicyName);
                endpoints.MapRazorPages();
            });
        }
    }
}
