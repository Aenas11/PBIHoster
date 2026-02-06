using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportTree.Server.Persistance;
using ReportTree.Server.Services;

namespace ReportTree.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrandingController : ControllerBase
{
    private readonly IBrandingAssetRepository _assets;
    private readonly BrandingService _brandingService;

    public BrandingController(IBrandingAssetRepository assets, BrandingService brandingService)
    {
        _assets = assets;
        _brandingService = brandingService;
    }

    [HttpGet("assets/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAsset(string id)
    {
        var asset = await _assets.GetAsync(id);
        if (asset == null)
        {
            return NotFound();
        }

        return File(asset.Content, asset.Info.ContentType, asset.Info.FileName);
    }

    [HttpPost("assets/{assetType}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> UploadAsset(string assetType, IFormFile file)
    {
        var username = User.Identity?.Name ?? "Unknown";
        var (result, error) = await _brandingService.UploadAssetAsync(assetType, file, username);
        if (result == null)
        {
            return BadRequest(new { error });
        }

        return Ok(result);
    }

    [HttpDelete("assets/{assetType}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> DeleteAsset(string assetType)
    {
        var username = User.Identity?.Name ?? "Unknown";
        var removed = await _brandingService.ClearAssetAsync(assetType, username);
        if (!removed)
        {
            return BadRequest(new { error = "Unsupported asset type." });
        }

        return Ok(new { removed = true });
    }
}
