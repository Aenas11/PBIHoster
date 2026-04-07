using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public interface IPageVersionRepository
{
    Task<int> CreateAsync(PageVersion version);
    Task<IEnumerable<PageVersion>> GetByPageIdAsync(int pageId, int take = 50);
    Task<PageVersion?> GetByIdAsync(int id);
}
