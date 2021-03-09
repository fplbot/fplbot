using System.Threading.Tasks;

namespace FplBot.Data.Abstractions
{
    public interface IIndexBookmarkProvider
    {
        Task<int> GetBookmark();
        Task SetBookmark(int bookmark);
    }
}
