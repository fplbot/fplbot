using System.Collections.Generic;
using System.Threading.Tasks;
using Dork.Models;

namespace Dork.Abstractions
{
    public interface ISelectEventHandlers
    {
        Task<IEnumerable<IHandleEvent>> GetEventHandlerFor(EventMetaData eventMetadata, SlackEvent slackEvent);
    }
}