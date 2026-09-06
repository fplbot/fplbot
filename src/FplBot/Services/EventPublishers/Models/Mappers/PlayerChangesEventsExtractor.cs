using Fpl.Client.Models;
using Fpl.EventPublishers.Models.Comparers;
using FplBot.Messaging.Contracts.Events.v1;

namespace Fpl.EventPublishers.Models.Mappers;

public static class PlayerChangesEventsExtractor
{

    public static IEnumerable<PlayerWithPriceChange> GetPriceChanges(ICollection<Player> after, ICollection<Player> players, ICollection<Team> teams)
    {
        if(players == null)
            return new List<PlayerWithPriceChange>();

        if (after == null)
            return new List<PlayerWithPriceChange>();

        var compared = ComparePlayers(after, players, teams, new PlayerPriceComparer());

        return compared.Select(p => new PlayerWithPriceChange
        (
            p.ToPlayer.Id,
            p.ToPlayer.WebName ?? string.Empty,
            p.ToPlayer.CostChangeEvent,
            p.ToPlayer.NowCost,
            p.ToPlayer.OwnershipPercentage,
            p.Team?.Id ?? 0,
            p.Team?.ShortName ?? string.Empty
        ));
    }

    public static IEnumerable<InjuredPlayerUpdate> GetInjuryUpdates(ICollection<Player> after, ICollection<Player> players, ICollection<Team> teams)
    {
        if(players == null)
            return new List<InjuredPlayerUpdate>();

        if (after == null)
            return new List<InjuredPlayerUpdate>();

        return CompareInjuredPlayers(after, players, teams, new StatusComparer());
    }

    public static IEnumerable<NewPlayer> GetNewPlayers(ICollection<Player> after, ICollection<Player> players, ICollection<Team> teams)
    {
        if (players == null)
            return new List<NewPlayer>();
        if (after == null)
            return new List<NewPlayer>();

        var diff = after.Except(players, new PlayerIdComparer());

        if (!diff.Any())
            return new List<NewPlayer>();

        var updates = diff.Select(newPlayer => new NewPlayer
        (
            newPlayer.Id,
            newPlayer.WebName ?? string.Empty,
            newPlayer.NowCost,
            teams.FirstOrDefault(t => t.Code == newPlayer.TeamCode)?.Id ?? 0,
            teams.FirstOrDefault(t => t.Code == newPlayer.TeamCode)?.Name ?? string.Empty
        ));
        return updates;
    }

    public static IEnumerable<InternalPremiershipTransfer> GetInternalPLTransfers(ICollection<Player> after,
        ICollection<Player> players, ICollection<Team> teams)
    {
        if (players == null)
            return new List<InternalPremiershipTransfer>();
        if (after == null)
            return new List<InternalPremiershipTransfer>();

        var diff = after.Except(players, new PlayersTeamChangeComparer());

        if (!diff.Any())
            return new List<InternalPremiershipTransfer>();

        var updates = new List<InternalPremiershipTransfer>();
        foreach (var player in diff)
        {
            var fromPlayer = players.FirstOrDefault(p => p.Id == player.Id);
            var afterPlayer = after.FirstOrDefault(p => p.Id == player.Id);
            var fromTeam = teams.FirstOrDefault(t => t.Code == fromPlayer?.TeamCode);
            var toTeam = teams.FirstOrDefault(t => t.Code == afterPlayer?.TeamCode);

            if (fromTeam != null && toTeam != null)
            {
                updates.Add(new InternalPremiershipTransfer(player.WebName ?? string.Empty, fromTeam.ShortName ?? string.Empty, toTeam.ShortName ?? string.Empty));
            }

        }
        return updates;
    }

    private static IEnumerable<PlayerUpdate> ComparePlayers(ICollection<Player> after, ICollection<Player> players, ICollection<Team> teams, IEqualityComparer<Player> changeComparer)
    {
        var playersWithChanges = after.Except(players, changeComparer).ToList();
        var updates = new List<PlayerUpdate>();
        foreach (var player in playersWithChanges)
        {
            var fromPlayer = players.FirstOrDefault(p => p.Id == player.Id);
            if (fromPlayer != null)
            {
                updates.Add(new PlayerUpdate
                {
                    FromPlayer = fromPlayer,
                    ToPlayer = player,
                    Team = teams.FirstOrDefault(t => t.Code == player.TeamCode),
                });
            }

        }

        return updates;
    }

    private static IEnumerable<InjuredPlayerUpdate> CompareInjuredPlayers(ICollection<Player> after, ICollection<Player> players, ICollection<Team> teams, IEqualityComparer<Player> changeComparer)
    {
        var playersWithChanges = after.Except(players, changeComparer).ToList();
        var updates = new List<InjuredPlayerUpdate>();
        foreach (var player in playersWithChanges)
        {
            var fromPlayer = players.FirstOrDefault(p => p.Id == player.Id);
            if (fromPlayer != null)
            {
                var team = teams.FirstOrDefault(t => t.Code == player.TeamCode);
                updates.Add(new InjuredPlayerUpdate
                (
                    new InjuredPlayer(fromPlayer.Id, fromPlayer.WebName ?? string.Empty, fromPlayer.OwnershipPercentage, new TeamDescription(team?.Id ?? 0, team?.ShortName ?? string.Empty, team?.Name ?? string.Empty)),
                    new InjuryStatus(fromPlayer.Status ?? string.Empty, fromPlayer.News ?? string.Empty),
                    new InjuryStatus(player.Status ?? string.Empty, player.News ?? string.Empty)
                ));
            }
        }

        return updates;
    }
}
