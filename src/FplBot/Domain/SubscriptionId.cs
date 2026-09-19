namespace FplBot.Domain;

public record SubscriptionId(string Value)
{
    public static SubscriptionId New() => new(Guid.NewGuid().ToString("N"));

    public override string ToString() => Value;
}
