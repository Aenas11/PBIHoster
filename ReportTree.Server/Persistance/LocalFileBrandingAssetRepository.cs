using System.Text.Json;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public class LocalFileBrandingAssetRepository : IBrandingAssetRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _basePath;

    public LocalFileBrandingAssetRepository(string basePath)
    {
        _basePath = basePath;
        Directory.CreateDirectory(_basePath);
    }

    public Task<BrandingAsset?> GetAsync(string id)
    {
        var info = ReadMetadata(id);
        if (info == null)
        {
            return Task.FromResult<BrandingAsset?>(null);
        }

        var stream = File.OpenRead(GetContentPath(id));
        return Task.FromResult<BrandingAsset?>(new BrandingAsset(info, stream));
    }

    public Task<BrandingAssetInfo?> GetInfoAsync(string id)
    {
        return Task.FromResult(ReadMetadata(id));
    }

    public async Task<BrandingAssetInfo> UploadAsync(string id, string fileName, string contentType, Stream content)
    {
        var info = new BrandingAssetInfo(
            id,
            fileName,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            0,
            DateTime.UtcNow);

        var contentPath = GetContentPath(id);
        await using (var output = File.Create(contentPath))
        {
            await content.CopyToAsync(output);
        }

        var fileInfo = new FileInfo(contentPath);
        var persistedInfo = info with { Length = fileInfo.Length };
        await File.WriteAllTextAsync(GetMetadataPath(id), JsonSerializer.Serialize(persistedInfo, JsonOptions));

        return persistedInfo;
    }

    public Task<bool> DeleteAsync(string id)
    {
        var deleted = false;
        var contentPath = GetContentPath(id);
        var metadataPath = GetMetadataPath(id);

        if (File.Exists(contentPath))
        {
            File.Delete(contentPath);
            deleted = true;
        }

        if (File.Exists(metadataPath))
        {
            File.Delete(metadataPath);
            deleted = true;
        }

        return Task.FromResult(deleted);
    }

    private BrandingAssetInfo? ReadMetadata(string id)
    {
        var metadataPath = GetMetadataPath(id);
        var contentPath = GetContentPath(id);
        if (!File.Exists(metadataPath) || !File.Exists(contentPath))
        {
            return null;
        }

        var json = File.ReadAllText(metadataPath);
        return JsonSerializer.Deserialize<BrandingAssetInfo>(json, JsonOptions);
    }

    private string GetContentPath(string id) => Path.Combine(_basePath, $"{id}.bin");

    private string GetMetadataPath(string id) => Path.Combine(_basePath, $"{id}.meta.json");
}