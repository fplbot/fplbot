using NServiceBus;

namespace FplBot.Functions.Messaging.Internal;

public class PublishViaWebHook : ICommand
{
    public string Message { get; }

    public PublishViaWebHook(string Message)
    {
        this.Message = Message;
    }
}
