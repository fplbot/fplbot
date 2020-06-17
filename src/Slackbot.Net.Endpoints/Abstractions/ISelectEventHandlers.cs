using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Models;

namespace Slackbot.Net.Endpoints.Abstractions
{
    public interface ISelectEventHandlers
    {
        Task<IEnumerable<IHandleEvent>> GetEventHandlerFor(EventMetaData eventMetadata, SlackEvent slackEvent);
    }
}