using LiteDB;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public class LiteDbPageVersionRepository : IPageVersionRepository
{
    private readonly ILiteCollection<PageVersion> _collection;

    public LiteDbPageVersionRepository(LiteDatabase db)
    {
        _collection = db.GetCollection<PageVersion>("page_versions");
        _collection.EnsureIndex(x => x.PageId);
        _collection.EnsureIndex(x => x.ChangedAt);
    }

    public Task<int> CreateAsync(PageVersion version)
    {
        version.ChangedAt = DateTime.UtcNow;
        var id = _collection.Insert(version);
        return Task.FromResult(id.AsInt32);
    }

    public Task<IEnumerable<PageVersion>> GetByPageIdAsync(int pageId, int take = 50)
    {
        var versions = _collection
            .Query()
            .Where(x => x.PageId == pageId)
            .OrderByDescending(x => x.ChangedAt)
            .Limit(Math.Max(1, take))
            .ToEnumerable();

        return Task.FromResult(versions);
    }

    public Task<PageVersion?> GetByIdAsync(int id)
    {
        var version = _collection.FindById(id) as PageVersion;
        return Task.FromResult<PageVersion?>(version);
    }
}
