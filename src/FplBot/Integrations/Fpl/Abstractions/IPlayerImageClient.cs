namespace Fpl.Client.Abstractions;

public interface IPlayerImageClient
{
    Task<string> GetPlayerImageUrl(int playerCode);
}
