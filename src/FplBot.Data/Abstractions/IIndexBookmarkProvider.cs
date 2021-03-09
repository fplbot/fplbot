using System.Threading.Tasks;

namespace Fpl.Data.Abstractions
{
    public interface IIndexBookmarkProvider
    {
        Task<int> GetBookmark();
        Task SetBookmark(int bookmark);
    }
}
