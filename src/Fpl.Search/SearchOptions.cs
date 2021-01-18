namespace Fpl.Search
{
    public class SearchOptions
    {
        public string IndexUri { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public void Validate()
        {
            if (string.IsNullOrEmpty(IndexUri) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                throw new FplSearchException("Misconfigured auth. Check config. IndexUri, Username or Password was empty");
        }
    }
}
