namespace FplBot.Domain;

public record WebPushSubscriberId(string Value)
{
    public static WebPushSubscriberId New() => new(Guid.NewGuid().ToString("N"));

    public override string ToString() => Value;
}
