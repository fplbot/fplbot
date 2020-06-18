using Dork.Abstractions;

namespace Dork.Hosting
{
    public interface ISlackbotEventHandlersBuilder
    {
        public ISlackbotEventHandlersBuilder AddHandler<T>() where T:class,IHandleEvent;
    }
}