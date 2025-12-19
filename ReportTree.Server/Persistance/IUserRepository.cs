using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance
{
    public interface IUserRepository
    {
        Task UpsertAsync(AppUser user);
        Task<AppUser?> GetByUsernameAsync(string username);
        Task<IEnumerable<AppUser>> SearchAsync(string term);
        Task DeleteAsync(string username);
        Task<int> CountAsync();
    }
}
