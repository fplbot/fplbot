using Fpl.Client.Abstractions;
using Fpl.Client.Models;


namespace Fpl.Client;

public class FixtureClient(HttpClient client) : IFixtureClient
{
    public async Task<ICollection<Fixture>?> GetFixtures()
    {
        return await client.GetFromJsonAsync<ICollection<Fixture>>("/api/fixtures/", JsonConvert.JsonSerializerOptions);
    }

    public async Task<ICollection<Fixture>?> GetFixturesByGameweek(int id)
    {
        return await client.GetFromJsonAsync<ICollection<Fixture>>($"/api/fixtures/?event={id}", JsonConvert.JsonSerializerOptions);
    }
}
