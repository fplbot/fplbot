using Fpl.Client.Models;

namespace Fpl.Client.Abstractions;

public interface IFixtureClient
{
    Task<ICollection<Fixture>> GetFixtures();

    Task<ICollection<Fixture>> GetFixturesByGameweek(int id);
}