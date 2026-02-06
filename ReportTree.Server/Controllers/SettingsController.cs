using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using ReportTree.Server.DTOs;
using ReportTree.Server.Services;
using System.IO;

namespace ReportTree.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CanManageUsers")]
public class SettingsController : ControllerBase
{
    private readonly SettingsService _settingsService;
    private readonly AuditLogService _auditLogService;
    private readonly IWebHostEnvironment _env;

    public SettingsController(SettingsService settingsService, AuditLogService auditLogService, IWebHostEnvironment env)
    {
        _settingsService = settingsService;
        _auditLogService = auditLogService;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var settings = await _settingsService.GetAllSettingsAsync();
        var dtos = settings.Select(s => new SettingDto(
            s.Key,
            s.IsEncrypted ? "***" : s.Value, // Mask encrypted values
            s.Category,
            s.Description,
            s.IsEncrypted
        ));
        return Ok(dtos);
    }

    [HttpGet("static")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStaticSettings()
    {
        var staticSettings = new Dictionary<string, string?>();
        staticSettings["HomePageId"] = await _settingsService.GetValueAsync("App.HomePageId");
        staticSettings["DemoModeEnabled"] = (await _settingsService.IsDemoModeEnabledAsync()).ToString();
        var appName = await _settingsService.GetValueAsync("Branding.AppName");
        var footerText = await _settingsService.GetValueAsync("Branding.FooterText");
        var footerLinkUrl = await _settingsService.GetValueAsync("Branding.FooterLinkUrl");
        var footerLinkLabel = await _settingsService.GetValueAsync("Branding.FooterLinkLabel");
        var logoAssetId = await _settingsService.GetValueAsync("Branding.LogoAssetId");
        var faviconAssetId = await _settingsService.GetValueAsync("Branding.FaviconAssetId");

        staticSettings["AppName"] = appName;
        staticSettings["FooterText"] = footerText;
        staticSettings["FooterLinkUrl"] = footerLinkUrl;
        staticSettings["FooterLinkLabel"] = footerLinkLabel;
        staticSettings["LogoUrl"] = string.IsNullOrWhiteSpace(logoAssetId) ? null : $"/api/branding/assets/{logoAssetId}";
        staticSettings["FaviconUrl"] = string.IsNullOrWhiteSpace(faviconAssetId) ? null : $"/api/branding/assets/{faviconAssetId}";
        staticSettings["Version"] = ReadVersion();
        return Ok(staticSettings);
    }

    private string ReadVersion()
    {
        try
        {
            var versionPath = Path.Combine(_env.ContentRootPath, "VERSION");
            return System.IO.File.Exists(versionPath) ? System.IO.File.ReadAllText(versionPath).Trim() : "0.0.0";
        }
        catch
        {
            return "0.0.0";
        }
    }

    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var settings = await _settingsService.GetByCategoryAsync(category);
        var dtos = settings.Select(s => new SettingDto(
            s.Key,
            s.IsEncrypted ? "***" : s.Value,
            s.Category,
            s.Description,
            s.IsEncrypted
        ));
        return Ok(dtos);
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetByKey(string key)
    {
        var setting = await _settingsService.GetSettingAsync(key);
        if (setting == null)
        {
            return NotFound();
        }
        var dto = new SettingDto(
            setting.Key,
            setting.IsEncrypted ? "***" : setting.Value,
            setting.Category,
            setting.Description,
            setting.IsEncrypted
        );
        return Ok(dto);
    }

    [HttpPut]
    public async Task<IActionResult> Upsert([FromBody] SettingUpdateDto dto)
    {
        var username = User.Identity?.Name ?? "Unknown";
        
        // Check if this is a sensitive setting (like encryption keys)
        var isEncrypted = dto.Category == "Security" || 
                          dto.Key.Contains("Key") || 
                          dto.Key.Contains("Secret") || 
                          dto.Key.Contains("Password");
        
        await _settingsService.UpsertSettingAsync(
            dto.Key,
            dto.Value,
            dto.Category,
            dto.Description,
            isEncrypted,
            username
        );

        await _auditLogService.LogAsync("UPDATE", $"Setting:{dto.Key}", $"Updated setting in category {dto.Category}");
        
        return Ok();
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key)
    {
        await _settingsService.DeleteSettingAsync(key);
        await _auditLogService.LogAsync("DELETE", $"Setting:{key}", "Deleted setting");
        return Ok();
    }
}
