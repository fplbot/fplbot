using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Models.Events;

namespace Slackbot.Net.Endpoints.Abstractions
{
    public interface IHandleAppHomeOpened
    {
        Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppHomeOpenedEvent payload);
    }
}