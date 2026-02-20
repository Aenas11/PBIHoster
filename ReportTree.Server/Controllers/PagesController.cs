using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ReportTree.Server.Models;
using ReportTree.Server.Persistance;
using ReportTree.Server.Services;
using System.Text.Json;

namespace ReportTree.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PagesController : ControllerBase
    {
        private readonly IPageRepository _repo;
        private readonly PageAuthorizationService _authService;
        private readonly AuthService _authServiceCore;
        private readonly IMemoryCache _cache;
        private readonly SettingsService _settingsService;
        private readonly DemoContentService _demoContentService;
        private readonly AuditLogService _auditLogService;
        private const string PAGES_CACHE_KEY = "all_pages";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public PagesController(
            IPageRepository repo,
            PageAuthorizationService authService,
            AuthService authServiceCore,
            IMemoryCache cache,
            SettingsService settingsService,
            DemoContentService demoContentService,
            AuditLogService auditLogService)
        {
            _repo = repo;
            _authService = authService;
            _authServiceCore = authServiceCore;
            _cache = cache;
            _settingsService = settingsService;
            _demoContentService = demoContentService;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "*" })]
        public async Task<IEnumerable<Page>> Get()
        {
            var isDemoMode = await _settingsService.IsDemoModeEnabledAsync();

            // Try to get from cache first
            if (!_cache.TryGetValue(PAGES_CACHE_KEY, out IEnumerable<Page>? allPages))
            {
                allPages = await _repo.GetAllAsync();
                _cache.Set(PAGES_CACHE_KEY, allPages, CacheDuration);
            }

            var pages = allPages?.ToList() ?? new List<Page>();

            if (isDemoMode)
            {
                pages.AddRange(_demoContentService.GetDemoPages());
            }

            // Filter based on user permissions
            return _authService.FilterAccessiblePages(pages, User);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "id" })]
        public async Task<ActionResult<Page>> Get(int id)
        {
            var isDemoMode = await _settingsService.IsDemoModeEnabledAsync();
            var page = await _repo.GetByIdAsync(id);

            if (page == null && isDemoMode)
            {
                page = _demoContentService.GetDemoPages().FirstOrDefault(p => p.Id == id);
            }

            if (page == null) return NotFound();

            if (!_authService.CanAccessPage(page, User))
            {
                return User.Identity?.IsAuthenticated == true ? Forbid() : Unauthorized();
            }

            await ApplyDefaultHomeLayoutAsync(page);

            return page;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<ActionResult<Page>> Post([FromBody] Page page)
        {
            var id = await _repo.CreateAsync(page);
            page.Id = id;
            
            // Invalidate cache
            _cache.Remove(PAGES_CACHE_KEY);
            
            return CreatedAtAction(nameof(Get), new { id = page.Id }, page);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Put(int id, [FromBody] Page page)
        {
            if (id != page.Id) return BadRequest();
            await _repo.UpdateAsync(page);
            
            // Invalidate cache
            _cache.Remove(PAGES_CACHE_KEY);
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            
            // Invalidate cache
            _cache.Remove(PAGES_CACHE_KEY);
            
            return NoContent();
        }

        [HttpPost("{id}/layout")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> SaveLayout(int id, [FromBody] object layout)
        {
            var page = await _repo.GetByIdAsync(id);
            if (page == null) return NotFound();

            var oldLayout = page.Layout;
            
            // Serialize the layout object to string for storage
            var layoutJson = JsonSerializer.Serialize(layout);
            page.Layout = layoutJson;
            await _repo.UpdateAsync(page);

            // Invalidate cache
            _cache.Remove(PAGES_CACHE_KEY);

            // Audit log: track RLS changes in detail
            try
            {
                var oldRlsComponents = ExtractRlsComponents(oldLayout ?? "[]");
                var newRlsComponents = ExtractRlsComponents(layoutJson);
                
                var changes = new List<string>();
                
                // Check for new RLS enabled components
                foreach (var newComp in newRlsComponents)
                {
                    var oldComp = oldRlsComponents.FirstOrDefault(c => c.Id == newComp.Id);
                    if (oldComp.Id == null)
                    {
                        // Previously had no RLS config
                        changes.Add($"RLS_ENABLED: component={newComp.Id}, roles=[{string.Join(",", newComp.Roles)}]");
                    }
                    else if (!ListsEqual(oldComp.Roles, newComp.Roles))
                    {
                        // RLS roles changed
                        changes.Add($"RLS_ROLES_CHANGED: component={newComp.Id}, from=[{string.Join(",", oldComp.Roles)}] to=[{string.Join(",", newComp.Roles)}]");
                    }
                }
                
                // Check for RLS disabled components
                foreach (var oldComp in oldRlsComponents)
                {
                    if (!newRlsComponents.Any(c => c.Id == oldComp.Id))
                    {
                        changes.Add($"RLS_DISABLED: component={oldComp.Id}, was=[{string.Join(",", oldComp.Roles)}]");
                    }
                }
                
                if (changes.Count > 0)
                {
                    var changeDetail = string.Join("; ", changes);
                    await _auditLogService.LogAsync("RLS_CONFIG_CHANGED", id.ToString(), changeDetail);
                }
            }
            catch
            {
                // Non-critical: swallow parsing errors so layout save is not affected
            }

            return Ok(new { success = true, message = "Layout saved successfully" });
        }

        private static List<(string Id, List<string> Roles)> ExtractRlsComponents(string layoutJson)
        {
            var result = new List<(string, List<string>)>();
            try
            {
                using var doc = JsonDocument.Parse(layoutJson);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) return result;
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (!item.TryGetProperty("componentConfig", out var config)) continue;
                    if (!config.TryGetProperty("enableRLS", out var enableRls) || !enableRls.GetBoolean()) continue;
                    var itemId = item.TryGetProperty("i", out var iEl) ? iEl.GetString() ?? "" : "";
                    var roles = new List<string>();
                    if (config.TryGetProperty("rlsRoles", out var rlsRoles) && rlsRoles.ValueKind == JsonValueKind.Array)
                    {
                        roles.AddRange(rlsRoles.EnumerateArray().Select(r => r.GetString() ?? "").Where(r => r.Length > 0));
                    }
                    result.Add((itemId, roles));
                }
            }
            catch { /* ignore parse errors */ }
            return result;
        }
        
        private static bool ListsEqual(List<string> list1, List<string> list2)
        {
            if (list1.Count != list2.Count) return false;
            return list1.OrderBy(x => x).SequenceEqual(list2.OrderBy(x => x));
        }

        [HttpPost("{id}/clone")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<ActionResult<Page>> ClonePage(int id, [FromBody] ClonePageRequest? request = null)
        {
            var sourcePage = await _repo.GetByIdAsync(id);
            if (sourcePage == null) return NotFound("Source page not found");

            // Create a new page with cloned properties
            var clonedPage = new Page
            {
                Title = request?.NewTitle ?? $"{sourcePage.Title} (Copy)",
                Icon = sourcePage.Icon,
                ParentId = request?.NewParentId ?? sourcePage.ParentId,
                Order = sourcePage.Order + 1, // Place right after original
                IsPublic = sourcePage.IsPublic,
                Layout = sourcePage.Layout, // Clone the entire layout configuration
                AllowedUsers = new List<string>(sourcePage.AllowedUsers),
                AllowedGroups = new List<string>(sourcePage.AllowedGroups),
                PowerBIWorkspaceId = sourcePage.PowerBIWorkspaceId
            };

            var newId = await _repo.CreateAsync(clonedPage);
            clonedPage.Id = newId;
            
            // Invalidate cache
            _cache.Remove(PAGES_CACHE_KEY);
            
            return CreatedAtAction(nameof(Get), new { id = clonedPage.Id }, clonedPage);
        }

        private async Task ApplyDefaultHomeLayoutAsync(Page page)
        {
            if (page.Layout != null && page.Layout.Trim().Length > 0)
            {
                return;
            }

            if (User.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var username = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username))
            {
                return;
            }

            var user = await _authServiceCore.GetUserAsync(username);
            if (user == null || user.HomeFavoritesSeeded)
            {
                return;
            }

            var homePageIdRaw = await _settingsService.GetValueAsync("App.HomePageId");
            if (!int.TryParse(homePageIdRaw, out var homePageId))
            {
                return;
            }

            if (homePageId != page.Id)
            {
                return;
            }

            page.Layout = BuildDefaultHomeLayout();
            await _repo.UpdateAsync(page);
            _cache.Remove(PAGES_CACHE_KEY);

            user.HomeFavoritesSeeded = true;
            await _authServiceCore.UpdateUserAsync(user);
        }

        private static string BuildDefaultHomeLayout()
        {
            var layout = new[]
            {
                new
                {
                    i = "panel-0",
                    x = 0,
                    y = 0,
                    w = 6,
                    h = 6,
                    minW = 3,
                    minH = 4,
                    componentType = "favorites",
                    componentConfig = new
                    {
                        showFavorites = true,
                        showRecents = true,
                        maxItems = 6
                    },
                    metadata = new
                    {
                        title = "Favorites & Recents",
                        description = "Quick access to favorite and recently viewed pages",
                        createdAt = DateTime.UtcNow.ToString("o"),
                        updatedAt = DateTime.UtcNow.ToString("o")
                    }
                }
            };

            return JsonSerializer.Serialize(layout);
        }
    }

    public record ClonePageRequest(string? NewTitle, int? NewParentId);
}
