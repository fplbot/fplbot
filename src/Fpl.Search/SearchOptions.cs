namespace Fpl.Search
{
    public class SearchOptions
    {
        public string IndexUri { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string EntriesIndex { get; set; }
        public string LeaguesIndex { get; set; }
        public bool ShouldIndex { get; set; }
        public string IndexingCron { get; set; }

        public void Validate()
        {
            if (string.IsNullOrEmpty(IndexUri) || 
                string.IsNullOrEmpty(Username) || 
                string.IsNullOrEmpty(Password) ||
                string.IsNullOrEmpty(EntriesIndex) ||
                string.IsNullOrEmpty(LeaguesIndex) || 
                (ShouldIndex && string.IsNullOrEmpty(IndexingCron)))
                throw new FplSearchException("Misconfigured search config");
        }
    }
}
