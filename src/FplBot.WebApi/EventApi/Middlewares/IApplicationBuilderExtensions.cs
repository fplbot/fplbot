using FplBot.WebApi.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.WebApi.EventApi.Middlewares
{
    public static class IApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseSlackbotEvents(this IApplicationBuilder app, string path)
        {
            app.MapWhen(ctx => SlackEventsChallengeMiddleware.ShouldRun(ctx, path), a => a.UseMiddleware<SlackEventsChallengeMiddleware>());
            app.MapWhen(ctx => SlackEventsUninstalledMiddleware.ShouldRun(ctx, path), a => a.UseMiddleware<SlackEventsUninstalledMiddleware>());
            app.MapWhen(ctx => SlackEventsMiddleware.ShouldRun(ctx, path), a => a.UseMiddleware<SlackEventsMiddleware>());
            return app;
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static ISlackbotEventHandlersBuilder AddSlackBotEventHandlers(this IServiceCollection services)
        {
            services.AddSingleton<ISelectEventHandlers, EventHandlerSelector>();
            return new SlackBotEventHandlersBuilder(services);
        }
    }

    public interface ISlackbotEventHandlersBuilder
    {
        public void AddEventHandler<T>() where T:class,IHandleEvent;
    }

    public class SlackBotEventHandlersBuilder : ISlackbotEventHandlersBuilder
    {
        private readonly IServiceCollection _services;

        public SlackBotEventHandlersBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public void AddEventHandler<T>() where T : class, IHandleEvent
        {
            _services.AddSingleton<IHandleEvent, T>();
        }
    }
}