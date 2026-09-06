namespace Fpl.PulseLive;

public interface IPulseLiveClient
{
    Task<MatchDetails?> GetMatchDetails(int pulseId);
}
