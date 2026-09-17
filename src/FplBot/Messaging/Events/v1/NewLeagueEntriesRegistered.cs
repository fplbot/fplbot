namespace FplBot.Messaging.Contracts.Events.v1;

public record NewLeagueEntriesRegistered(int LeagueId, string LeagueName, List<NewLeagueEntrant> NewEntries);

public record NewLeagueEntrant(int EntryId, string EntryName, string PlayerName);
