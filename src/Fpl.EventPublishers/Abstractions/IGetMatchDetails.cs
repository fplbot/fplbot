namespace Fpl.EventPublishers.Abstractions;

public interface IGetMatchDetails
{
    Task<MatchDetails> GetMatchDetails(int pulseId);
}
