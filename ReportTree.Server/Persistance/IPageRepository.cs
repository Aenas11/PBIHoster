using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance
{
    public interface IPageRepository
    {
        Task<IEnumerable<Page>> GetAllAsync();
        Task<Page?> GetByIdAsync(int id);
        Task<int> CreateAsync(Page page);
        Task UpdateAsync(Page page);
        Task DeleteAsync(int id);
    }
}
