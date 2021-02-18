using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace Slackbot.Net.Endpoints
{
    internal class AppMentionEventHandlerSelector : ISelectAppMentionEventHandlers
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServiceProvider _provider;

        public AppMentionEventHandlerSelector(ILoggerFactory loggerFactory, IServiceProvider provider)
        {
            _loggerFactory = loggerFactory;
            _provider = provider;
        }
        
        public async Task<IEnumerable<IHandleAppMentions>> GetAppMentionEventHandlerFor(EventMetaData eventMetadata, AppMentionEvent slackEvent)
        {
            var allHandlers = _provider.GetServices<IHandleAppMentions>();
            var shortCutter = _provider.GetService<IShortcutAppMentions>();
            var noopHandler = _provider.GetService<INoOpAppMentions>();

            if (shortCutter != null && shortCutter.ShouldShortcut(slackEvent))
            {
                await shortCutter.Handle(eventMetadata, slackEvent);
                return new List<IHandleAppMentions>();
            }

            return SelectHandler(allHandlers, noopHandler, slackEvent);
 
        }

        private IEnumerable<IHandleAppMentions> SelectHandler(IEnumerable<IHandleAppMentions> handlers, INoOpAppMentions noOpAppMentions, AppMentionEvent message)
        {
            var matchingHandlers = handlers.Where(s => s.ShouldHandle(message));
            if (matchingHandlers.Any())
                return matchingHandlers;
            
            if(noOpAppMentions != null)
                return new List<IHandleAppMentions> { noOpAppMentions };
            
            return new List<IHandleAppMentions>
            {
                new NoOpAppMentionEventHandler(_loggerFactory.CreateLogger<NoOpAppMentionEventHandler>())
            };
        }
    }
}