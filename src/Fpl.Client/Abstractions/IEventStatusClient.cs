using Fpl.Client.Models;

namespace Fpl.Client.Abstractions;

public interface IEventStatusClient
{
    Task<EventStatusResponse> GetEventStatus();
}