namespace Fpl.Search.Data.Abstractions;

public interface IIndexBookmarkProvider
{
    Task<int> GetBookmark();
    Task SetBookmark(int bookmark);
}

public interface ILeagueIndexBookmarkProvider : IIndexBookmarkProvider { }
public interface IEntryIndexBookmarkProvider : IIndexBookmarkProvider { }
