using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Models.Events;

namespace Slackbot.Net.Endpoints.Abstractions
{
    public interface IShortcutAppMentions
    {
        Task Handle(EventMetaData eventMetadata, AppMentionEvent @event);
        bool ShouldShortcut(AppMentionEvent @event);
    }
}