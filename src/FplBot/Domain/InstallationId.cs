namespace FplBot.Domain;

public record InstallationId(string Value)
{
    public static InstallationId New() => new(Guid.NewGuid().ToString("N"));

    public override string ToString() => Value;
}
