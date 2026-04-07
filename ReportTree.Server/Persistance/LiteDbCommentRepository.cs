using LiteDB;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public class LiteDbCommentRepository : ICommentRepository
{
    private readonly ILiteCollection<Comment> _collection;

    public LiteDbCommentRepository(LiteDatabase db)
    {
        _collection = db.GetCollection<Comment>("comments");
        _collection.EnsureIndex(x => x.PageId);
        _collection.EnsureIndex(x => x.ParentId);
        _collection.EnsureIndex(x => x.Username);
        _collection.EnsureIndex(x => x.CreatedAt);
    }

    public Task<IEnumerable<Comment>> GetByPageIdAsync(int pageId)
    {
        var items = _collection
            .Query()
            .Where(x => x.PageId == pageId)
            .OrderBy(x => x.CreatedAt)
            .ToEnumerable();

        return Task.FromResult(items);
    }

    public Task<Comment?> GetByIdAsync(int id)
    {
        var item = _collection.FindById(id) as Comment;
        return Task.FromResult<Comment?>(item);
    }

    public Task<int> CreateAsync(Comment comment)
    {
        comment.CreatedAt = DateTime.UtcNow;
        var id = _collection.Insert(comment);
        return Task.FromResult(id.AsInt32);
    }

    public Task UpdateAsync(Comment comment)
    {
        comment.UpdatedAt = DateTime.UtcNow;
        _collection.Update(comment);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id)
    {
        _collection.Delete(id);
        return Task.CompletedTask;
    }
}