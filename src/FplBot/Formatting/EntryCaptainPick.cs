using Fpl.Client.Models;

namespace FplBot.Formatting;

public class EntryCaptainPick
{
    public GenericEntry Entry { get; set; } = null!;
    public Player Captain { get; set; } = null!;
    public Player ViceCaptain { get; set; } = null!;
    public bool IsTripleCaptain { get; set; }
}
