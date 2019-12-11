using FplBot.ConsoleApps.Clients;

namespace FplBot.ConsoleApps
{
    public class FplApiClientOptions
    {
        public string Login { get; set; }
        public string Password { get; set; }

        public void Validate()
        {
            if(string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(Password))
                throw new FplApiException("Misconfigured auth. Check config. Username or Password was empty");
        }
    }
}