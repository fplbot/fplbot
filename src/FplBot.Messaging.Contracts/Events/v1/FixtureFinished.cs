using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1;

[TimeToBeReceived("00:15:00")] // discard events not being handled within 15 mins
public record FixtureFinished(int FixtureId) : IEvent;
