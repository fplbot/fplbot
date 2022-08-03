using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1;

public record PremiershipPlayerTransferred(List<InternalPremiershipTransfer> Transfers) : IEvent;

public record InternalPremiershipTransfer(string WebName, string FromTeam, string ToTeam);

