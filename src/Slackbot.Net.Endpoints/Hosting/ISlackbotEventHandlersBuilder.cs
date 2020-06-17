using Slackbot.Net.Endpoints.Abstractions;

namespace Slackbot.Net.Endpoints.Hosting
{
    public interface ISlackbotEventHandlersBuilder
    {
        public void AddEventHandler<T>() where T:class,IHandleEvent;
    }
}