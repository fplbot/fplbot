namespace Fpl.Client.Clients;

public class FplApiClientOptions
{
    public required string Login { get; set; }
    public required string Password { get; set; }

    public required string REDIS_URL { get; set; }

    public void Validate()
    {
        if(string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(Password))
            throw new FplApiException("Misconfigured auth. Check config. Username or Password was empty");
    }
}
