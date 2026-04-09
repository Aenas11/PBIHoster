using Microsoft.EntityFrameworkCore;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance.Relational;

public class EfCommentRepository : ICommentRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public EfCommentRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IEnumerable<Comment>> GetByPageIdAsync(int pageId)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.Comments
            .Where(x => x.PageId == pageId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<Comment?> GetByIdAsync(int id)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.Comments.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<int> CreateAsync(Comment comment)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        comment.CreatedAt = DateTime.UtcNow;
        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync();
        return comment.Id;
    }

    public async Task UpdateAsync(Comment comment)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        comment.UpdatedAt = DateTime.UtcNow;
        dbContext.Comments.Update(comment);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var comment = await dbContext.Comments.FirstOrDefaultAsync(x => x.Id == id);
        if (comment == null)
        {
            return;
        }

        dbContext.Comments.Remove(comment);
        await dbContext.SaveChangesAsync();
    }
}