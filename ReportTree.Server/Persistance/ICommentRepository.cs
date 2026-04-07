using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public interface ICommentRepository
{
    Task<IEnumerable<Comment>> GetByPageIdAsync(int pageId);
    Task<Comment?> GetByIdAsync(int id);
    Task<int> CreateAsync(Comment comment);
    Task UpdateAsync(Comment comment);
    Task DeleteAsync(int id);
}