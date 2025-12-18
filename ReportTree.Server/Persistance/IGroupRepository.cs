using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance
{
    public interface IGroupRepository
    {
        Task<IEnumerable<Group>> GetAllAsync();
        Task<IEnumerable<Group>> SearchAsync(string term);
        Task<int> CreateAsync(Group group);
        Task DeleteAsync(int id);
    }
}
