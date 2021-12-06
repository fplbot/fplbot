using Fpl.Client.Models;

namespace Fpl.Workers.Models;

public class PlayerUpdate
{
    public Player FromPlayer { get; set; }
    public Player ToPlayer { get; set; }
    public Team Team { get; set; }

    public void Deconstruct(out string fromStatus, out string toStatus)
    {
        fromStatus = FromPlayer?.Status;
        toStatus = ToPlayer?.Status;
    }
}
