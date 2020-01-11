using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Slackbot.Net.SlackClients.Http.Extensions;
using StackExchange.Redis;

namespace FplBot.WebApi
{
    public class Startup
    {

        private static readonly string REDIS_SERVER = Environment.GetEnvironmentVariable("REDIS_SERVER");
        private static readonly string REDIS_USERNAME = Environment.GetEnvironmentVariable("REDIS_USERNAME");
        private static readonly string REDIS_PASSWORD = Environment.GetEnvironmentVariable("REDIS_PASSWORD");
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSlackbotOauthAccessClient();
            services.AddSingleton<ConnectionMultiplexer>(ConnectionMultiplexer.Connect($"{REDIS_SERVER}, name={REDIS_USERNAME}, password={REDIS_PASSWORD}"));
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

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
