using System;
using System.Threading.Tasks;
using Fpl.Search.Indexing;

namespace Fpl.SearchConsole
{
    internal class SimpleLeagueIndexBookmarkProvider : IIndexBookmarkProvider
    {
        private string Path = "./bookmark.txt";

        public async Task<int> GetBookmark()
        {
            try
            {
                var txt = await System.IO.File.ReadAllTextAsync(Path);

                return int.TryParse(txt, out int bookmark) ? bookmark : 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 1;
            }
        }

        public Task SetBookmark(int bookmark)
        {
            try
            {
                Console.WriteLine($"Setting bookmark at {bookmark}.");
                return System.IO.File.WriteAllTextAsync(Path, bookmark.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Task.CompletedTask;
            }
        }
    }
}