using Fpl.Client.Models;
using MediatR;

namespace Fpl.EventPublishers.Events;

// Only used for internal state in Workers
internal record GameweekMonitoringStarted(Gameweek CurrentGameweek) : INotification;
internal record GameweekCurrentlyOnGoing(Gameweek Gameweek) : INotification;
internal record GameweekCurrentlyFinished(Gameweek Gameweek) : INotification;
internal record GameweekJustBegan(Gameweek Gameweek) : INotification;
internal record GameweekFinished(Gameweek Gameweek) : INotification;
