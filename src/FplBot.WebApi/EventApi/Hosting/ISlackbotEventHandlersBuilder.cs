namespace FplBot.WebApi.EventApi.Middlewares
{
    public interface ISlackbotEventHandlersBuilder
    {
        public void AddEventHandler<T>() where T:class,IHandleEvent;
    }
}