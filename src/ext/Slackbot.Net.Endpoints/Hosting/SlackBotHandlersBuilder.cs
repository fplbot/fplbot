using Microsoft.Extensions.DependencyInjection;
using Slackbot.Net.Endpoints.Abstractions;

namespace Slackbot.Net.Endpoints.Hosting
{
    public class SlackBotHandlersBuilder : ISlackbotHandlersBuilder
    {
        private readonly IServiceCollection _services;

        public SlackBotHandlersBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public ISlackbotHandlersBuilder AddAppMentionHandler<T>() where T : class, IHandleAppMentions
        {
            _services.AddSingleton<IHandleAppMentions, T>();
            return this;
        }

        public ISlackbotHandlersBuilder AddMemberJoinedChannelHandler<T>() where T : class, IHandleMemberJoinedChannel
        {
            _services.AddSingleton<IHandleMemberJoinedChannel, T>();
            return this;
        }

        public ISlackbotHandlersBuilder AddViewSubmissionHandler<T>() where T : class, IHandleViewSubmissions
        {
            _services.AddSingleton<IHandleViewSubmissions, T>();
            return this;
        }

        public ISlackbotHandlersBuilder AddAppHomeOpenedHandler<T>() where T : class, IHandleAppHomeOpened
        {
            _services.AddSingleton<IHandleAppHomeOpened, T>();
            return this;
        }

        public ISlackbotHandlersBuilder AddShortcut<T>() where T : class, IShortcutAppMentions
        {
            _services.AddSingleton<IShortcutAppMentions, T>();
            return this;
        }
    }
}