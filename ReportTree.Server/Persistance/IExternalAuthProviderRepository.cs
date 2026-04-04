using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public interface IExternalAuthProviderRepository
{
    Task<IReadOnlyList<ExternalAuthProvider>> GetAllAsync();
    Task<IReadOnlyList<ExternalAuthProvider>> GetEnabledAsync();
    Task<ExternalAuthProvider?> GetByIdAsync(string id);
}