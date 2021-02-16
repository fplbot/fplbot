using Fpl.Client.Models;
using MediatR;

namespace FplBot.Core.Models
{    
    // Only used for internal state in Workers
    internal record GameweekMonitoringStarted(Gameweek CurrentGameweek) : INotification;
    internal record GameweekCurrentlyOnGoing(Gameweek Gameweek) : INotification;
    internal record GameweekCurrentlyFinished(Gameweek Gameweek) : INotification;
}