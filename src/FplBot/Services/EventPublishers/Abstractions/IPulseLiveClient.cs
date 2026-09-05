namespace Fpl.EventPublishers.Abstractions;

public interface IPulseLiveClient
{
    Task<MatchDetails> GetMatchDetails(int pulseId);
}
