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
        private const string PAGES_CACHE_KEY = "all_pages";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public PagesController(IPageRepository repo, PageAuthorizationService authService, IMemoryCache cache)
        {
            _repo = repo;
            _authService = authService;
            _cache = cache;
        }

        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "*" })]
        public async Task<IEnumerable<Page>> Get()
        {
            // Try to get from cache first
            if (!_cache.TryGetValue(PAGES_CACHE_KEY, out IEnumerable<Page>? allPages))
            {
                allPages = await _repo.GetAllAsync();
                _cache.Set(PAGES_CACHE_KEY, allPages, CacheDuration);
            }

            // Filter based on user permissions
            return _authService.FilterAccessiblePages(allPages!, User);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "id" })]
        public async Task<ActionResult<Page>> Get(int id)
        {
            var page = await _repo.GetByIdAsync(id);
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
    }
}
