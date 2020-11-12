using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Models.Events;

namespace Slackbot.Net.Endpoints.Abstractions
{
    public interface ISelectAppMentionEventHandlers
    {
        Task<IEnumerable<IHandleAppMentions>> GetAppMentionEventHandlerFor(EventMetaData eventMetadata, AppMentionEvent slackEvent);
    }
}