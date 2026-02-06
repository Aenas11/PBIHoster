using Microsoft.AspNetCore.Http;
using ReportTree.Server.DTOs;
using ReportTree.Server.Persistance;

namespace ReportTree.Server.Services;

public class BrandingService
{
    private const long MaxAssetBytes = 2_000_000;
    private static readonly HashSet<string> LogoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/webp"
    };

    private static readonly HashSet<string> FaviconContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/x-icon",
        "image/vnd.microsoft.icon"
    };

    private readonly IBrandingAssetRepository _assets;
    private readonly SettingsService _settingsService;

    public BrandingService(IBrandingAssetRepository assets, SettingsService settingsService)
    {
        _assets = assets;
        _settingsService = settingsService;
    }

    public async Task<(BrandingAssetUploadResult? Result, string? Error)> UploadAssetAsync(string assetType, IFormFile file, string modifiedBy)
    {
        var assetKey = GetAssetKey(assetType);
        if (assetKey == null)
        {
            return (null, "Unsupported asset type.");
        }

        if (file == null || file.Length == 0)
        {
            return (null, "No file uploaded.");
        }

        if (file.Length > MaxAssetBytes)
        {
            return (null, "File exceeds the 2 MB size limit.");
        }

        if (!IsAllowedContentType(assetType, file.ContentType))
        {
            return (null, "Unsupported file type.");
        }

        var existingId = await _settingsService.GetValueAsync(assetKey);
        if (!string.IsNullOrWhiteSpace(existingId))
        {
            await _assets.DeleteAsync(existingId);
        }

        var assetId = Guid.NewGuid().ToString("N");
        await using var stream = file.OpenReadStream();
        var info = await _assets.UploadAsync(assetId, file.FileName, file.ContentType, stream);

        await _settingsService.UpsertSettingAsync(
            assetKey,
            info.Id,
            "Branding",
            assetType == "logo" ? "Brand logo asset id" : "Favicon asset id",
            false,
            modifiedBy
        );

        var url = $"/api/branding/assets/{info.Id}";
        return (new BrandingAssetUploadResult(info.Id, url), null);
    }

    public async Task<bool> ClearAssetAsync(string assetType, string modifiedBy)
    {
        var assetKey = GetAssetKey(assetType);
        if (assetKey == null)
        {
            return false;
        }

        var existingId = await _settingsService.GetValueAsync(assetKey);
        if (!string.IsNullOrWhiteSpace(existingId))
        {
            await _assets.DeleteAsync(existingId);
        }

        await _settingsService.UpsertSettingAsync(
            assetKey,
            string.Empty,
            "Branding",
            assetType == "logo" ? "Brand logo asset id" : "Favicon asset id",
            false,
            modifiedBy
        );

        return true;
    }

    private static string? GetAssetKey(string assetType)
    {
        return assetType.ToLowerInvariant() switch
        {
            "logo" => "Branding.LogoAssetId",
            "favicon" => "Branding.FaviconAssetId",
            _ => null
        };
    }

    private static bool IsAllowedContentType(string assetType, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return assetType.ToLowerInvariant() switch
        {
            "logo" => LogoContentTypes.Contains(contentType),
            "favicon" => FaviconContentTypes.Contains(contentType),
            _ => false
        };
    }
}
