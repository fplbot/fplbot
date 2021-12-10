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
            p.ToPlayer.WebName,
            p.ToPlayer.CostChangeEvent,
            p.ToPlayer.NowCost,
            p.ToPlayer.OwnershipPercentage,
            p.Team.Id,
            p.Team.ShortName
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
            newPlayer.WebName,
            newPlayer.NowCost,
            teams.FirstOrDefault(t => t.Code == newPlayer.TeamCode).Id,
            teams.FirstOrDefault(t => t.Code == newPlayer.TeamCode).Name
        ));
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
                Team team = teams.FirstOrDefault(t => t.Code == player.TeamCode);
                updates.Add(new InjuredPlayerUpdate
                (
                    new InjuredPlayer(fromPlayer.Id, fromPlayer.WebName, fromPlayer.OwnershipPercentage, new TeamDescription(team.Id, team.ShortName, team.Name)),
                    new InjuryStatus(fromPlayer.Status, fromPlayer.News),
                    new InjuryStatus(player.Status, player.News)
                ));
            }
        }

        return updates;
    }
}
