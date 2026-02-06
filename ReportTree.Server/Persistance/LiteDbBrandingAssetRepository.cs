using LiteDB;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public class LiteDbBrandingAssetRepository : IBrandingAssetRepository
{
    private readonly ILiteStorage<string> _storage;

    public LiteDbBrandingAssetRepository(LiteDatabase db)
    {
        _storage = db.FileStorage;
    }

    public Task<BrandingAsset?> GetAsync(string id)
    {
        var info = _storage.FindById(id);
        if (info == null)
        {
            return Task.FromResult<BrandingAsset?>(null);
        }

        var assetInfo = MapInfo(info);
        var stream = _storage.OpenRead(id);
        return Task.FromResult<BrandingAsset?>(new BrandingAsset(assetInfo, stream));
    }

    public Task<BrandingAssetInfo?> GetInfoAsync(string id)
    {
        var info = _storage.FindById(id);
        return Task.FromResult(info == null ? null : MapInfo(info));
    }

    public Task<BrandingAssetInfo> UploadAsync(string id, string fileName, string contentType, Stream content)
    {
        var metadata = new BsonDocument
        {
            ["contentType"] = contentType,
            ["uploadedAt"] = DateTime.UtcNow
        };

        _storage.Upload(id, fileName, content, metadata);

        var info = _storage.FindById(id);
        if (info == null)
        {
            throw new InvalidOperationException("Uploaded asset could not be found.");
        }

        return Task.FromResult(MapInfo(info));
    }

    public Task<bool> DeleteAsync(string id)
    {
        return Task.FromResult(_storage.Delete(id));
    }

    private static BrandingAssetInfo MapInfo(LiteFileInfo<string> info)
    {
        var contentType = info.Metadata?["contentType"].AsString ?? "application/octet-stream";
        var uploadedAt = info.Metadata?["uploadedAt"].IsDateTime == true
            ? info.Metadata["uploadedAt"].AsDateTime
            : info.UploadDate;

        return new BrandingAssetInfo(
            info.Id,
            info.Filename,
            contentType,
            info.Length,
            uploadedAt
        );
    }
}
