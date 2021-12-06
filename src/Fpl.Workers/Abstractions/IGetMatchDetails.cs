namespace Fpl.Workers.Abstractions;

public interface IGetMatchDetails
{
    Task<MatchDetails> GetMatchDetails(int pulseId);
}
