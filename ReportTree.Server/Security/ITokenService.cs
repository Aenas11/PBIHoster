using ReportTree.Server.Models;

namespace ReportTree.Server.Security
{
    public interface ITokenService
    {
        string Generate(AppUser user);
    }
}
