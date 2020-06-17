using System.Threading.Tasks;
using FplBot.WebApi.EventApi;

namespace FplBot.WebApi
{
    public class NoOpEventHandler : IHandleEvent
    {
        Task IHandleEvent.Handle(EventMetaData eventMetadata, SlackEvent slackEvent)
        {
            return Task.CompletedTask;
        }

        public bool ShouldHandle(SlackEvent slackEvent)
        {
            return true;
        }

        public bool ShouldShowInHelp => false;

        public (string, string) GetHelpDescription() => ("nada", "Fallback when no handlers are matched for any event you subscribe to");
    }
}