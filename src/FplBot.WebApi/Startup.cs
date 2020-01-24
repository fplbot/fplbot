using System;
using FplBot.WebApi.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
            services.AddSlackbotOauthAccessClient();
            services.Configure<RedisOptions>(Configuration);
            services.Configure<DistributedSlackAppOptions>(Configuration);
            services.AddSingleton<ConnectionMultiplexer>(c =>
            {
                var opts = c.GetService<IOptions<RedisOptions>>().Value;
                return ConnectionMultiplexer.Connect($"{opts.REDIS_SERVER}, name={opts.REDIS_USERNAME}, password={opts.REDIS_PASSWORD}");
            });
            services.AddSingleton<ISlackTeamRepository, RedisSlackTeamRepository>();
            services.AddSlackbotWorker<RedisSlackTeamRepository>()
                .AddSlackPublisher()
                .AddLoggerPublisher()
                .AddFplBot(Configuration.GetSection("fpl"))
                .BuildRecurrers();
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
