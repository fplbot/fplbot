using Bogus;
using FplBot.Domain;

namespace FplBot.Tests.Helpers;

public static class SlackInstallationFaker
{
    private static readonly Faker<Installation> Faker = new Faker<Installation>()
        .CustomInstantiator(f => Installation.Load("T" + f.Random.Replace("##########"), f.Company.CompanyName(), "xoxb-" + f.Random.AlphaNumeric(24), []));

    /// <summary>
    /// A fresh, uniquely-identified Installation per call, so tests sharing a real backing
    /// store (Redis) never collide on the same key even when xUnit runs them concurrently.
    /// </summary>
    public static Installation Generate() => Faker.Generate();
}
