using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public interface IBrandingAssetRepository
{
    Task<BrandingAsset?> GetAsync(string id);
    Task<BrandingAssetInfo?> GetInfoAsync(string id);
    Task<BrandingAssetInfo> UploadAsync(string id, string fileName, string contentType, Stream content);
    Task<bool> DeleteAsync(string id);
}
