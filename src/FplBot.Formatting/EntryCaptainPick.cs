using Fpl.Client.Models;

namespace FplBot.Formatting;

public class EntryCaptainPick
{
    public GenericEntry Entry { get; set; }
    public Player Captain { get; set; }
    public Player ViceCaptain { get; set; }
    public bool IsTripleCaptain { get; set; }
}