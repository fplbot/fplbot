using Fpl.Client.Models;

namespace Fpl.EventPublishers.Models;

public class PlayerUpdate
{
    public Player FromPlayer { get; set; } = null!;
    public Player ToPlayer { get; set; } = null!;
    public Team? Team { get; set; }

    public void Deconstruct(out string? fromStatus, out string? toStatus)
    {
        fromStatus = FromPlayer?.Status;
        toStatus = ToPlayer?.Status;
    }
}
