using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace FplBot.WebApi.EventApi
{
    public interface IHandleEvent
    {
        Task Handle(EventMetaData eventMetadata, SlackEvent slackEvent);
        bool ShouldHandle(SlackEvent slackEvent);

        bool ShouldShowInHelp => true;
        (string key, string description) GetHelpDescription();
    }
}