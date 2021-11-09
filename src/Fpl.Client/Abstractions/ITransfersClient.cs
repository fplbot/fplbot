using Fpl.Client.Models;

namespace Fpl.Client.Abstractions;

public interface ITransfersClient
{
    Task<ICollection<Transfer>> GetTransfers(int teamId);
}