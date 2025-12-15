using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance
{
    public interface IUserRepository
    {
        void Upsert(AppUser user, string plainPassword);
        AppUser? Validate(string username, string plainPassword);
    }
}
