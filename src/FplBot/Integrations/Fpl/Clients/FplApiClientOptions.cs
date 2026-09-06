namespace Fpl.Client.Clients;

public class FplApiClientOptions
{
    public required string Login { get; set; }
    public required string Password { get; set; }

    public required string REDIS_URL { get; set; }
}
