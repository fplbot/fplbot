namespace Discord.Net.HttpClients.Components;

public record ComponentRequest(IReadOnlyList<MessageComponent> Components)
{
    public const int IsComponentsV2 = 1 << 15;
}
