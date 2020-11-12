using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Models.Events;

namespace Slackbot.Net.Endpoints.Abstractions
{
    public interface IHandleAppMentions
    {
        Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent slackEvent);
        bool ShouldHandle(AppMentionEvent slackEvent);
        (string HandlerTrigger, string Description) GetHelpDescription() => ("", "");
    }
}