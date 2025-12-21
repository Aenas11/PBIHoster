using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ReportTree.Server.Models;
using ReportTree.Server.Persistance;
using ReportTree.Server.Services;

namespace ReportTree.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PagesController : ControllerBase
    {
        private readonly IPageRepository _repo;
        private readonly PageAuthorizationService _authService;
        private readonly IMemoryCache _cache;
        private readonly SettingsService _settingsService;
        private readonly DemoContentService _demoContentService;
        private const string PAGES_CACHE_KEY = "all_pages";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public PagesController(
            IPageRepository repo, 
            PageAuthorizationService authService,
            IMemoryCache cache,
            SettingsService settingsService,
            DemoContentService demoContentService)
        {
            _repo = repo;
            _authService = authService;
            _cache = cache;
            _settingsService = settingsService;
            _demoContentService = demoContentService;
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
            
            // Serialize the layout object to string for storage
            page.Layout = System.Text.Json.JsonSerializer.Serialize(layout);
            await _repo.UpdateAsync(page);
            
            // Invalidate cache
            _cache.Remove(PAGES_CACHE_KEY);
            
            return Ok(new { success = true, message = "Layout saved successfully" });
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
    }

    public record ClonePageRequest(string? NewTitle, int? NewParentId);
}
