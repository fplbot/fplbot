using System;
using System.Threading.Tasks;
using Fpl.Data.Abstractions;
using Fpl.Data.Repositories;
using Fpl.Search.Indexing;
using Microsoft.Extensions.Logging;

namespace Fpl.SearchConsole
{
    internal class SimpleLeagueIndexBookmarkProvider : IIndexBookmarkProvider
    {
        private readonly ILogger<SimpleLeagueIndexBookmarkProvider> _logger;
        private string Path = "./bookmark.txt";

        public SimpleLeagueIndexBookmarkProvider(ILogger<SimpleLeagueIndexBookmarkProvider> logger)
        {
            _logger = logger;
        }
        
        public async Task<int> GetBookmark()
        {
            try
            {
                var txt = await System.IO.File.ReadAllTextAsync(Path);

                return int.TryParse(txt, out int bookmark) ? bookmark : 1;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return 1;
            }
        }

        public Task SetBookmark(int bookmark)
        {
            try
            {
                _logger.LogInformation($"Setting bookmark at {bookmark}.");
                return System.IO.File.WriteAllTextAsync(Path, bookmark.ToString());
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return Task.CompletedTask;
            }
        }
    }
}