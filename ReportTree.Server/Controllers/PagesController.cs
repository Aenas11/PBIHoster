using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportTree.Server.Models;
using ReportTree.Server.Persistance;
using System.Security.Claims;

namespace ReportTree.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PagesController : ControllerBase
    {
        private readonly IPageRepository _repo;

        public PagesController(IPageRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IEnumerable<Page>> Get()
        {
            var allPages = await _repo.GetAllAsync();
            
            var userRoles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
            var userGroups = User.Claims
                .Where(c => c.Type == "Group")
                .Select(c => c.Value)
                .ToList();
            var username = User.Identity?.Name;
            
            var isAdmin = userRoles.Contains("Admin");

            return allPages.Where(p => 
                p.IsPublic || 
                (User.Identity?.IsAuthenticated == true && (
                    isAdmin || 
                    (username != null && (p.AllowedUsers ?? new List<string>()).Contains(username)) ||
                    (p.AllowedGroups ?? new List<string>()).Any(g => userGroups.Contains(g))
                ))
            );
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Page>> Get(int id)
        {
            var page = await _repo.GetByIdAsync(id);
            if (page == null) return NotFound();

            var userRoles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
            var userGroups = User.Claims
                .Where(c => c.Type == "Group")
                .Select(c => c.Value)
                .ToList();
            var username = User.Identity?.Name;
            
            var isAdmin = userRoles.Contains("Admin");

            if (!page.IsPublic)
            {
                if (User.Identity?.IsAuthenticated != true) return Unauthorized();
                
                var hasAccess = isAdmin || 
                                (username != null && (page.AllowedUsers ?? new List<string>()).Contains(username)) ||
                                (page.AllowedGroups ?? new List<string>()).Any(g => userGroups.Contains(g));

                if (!hasAccess)
                {
                    return Forbid();
                }
            }

            return page;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<ActionResult<Page>> Post([FromBody] Page page)
        {
            var id = await _repo.CreateAsync(page);
            page.Id = id;
            return CreatedAtAction(nameof(Get), new { id = page.Id }, page);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Put(int id, [FromBody] Page page)
        {
            if (id != page.Id) return BadRequest();
            await _repo.UpdateAsync(page);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("{id}/layout")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> SaveLayout(int id, [FromBody] object layout)
        {
            var page = await _repo.GetByIdAsync(id);
            if (page == null) return NotFound();

            // Ensure user has permission to edit this page
            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            var isAdmin = userRoles.Contains("Admin");
            // Assuming Editors can edit any page, or we might want to restrict to page owners if that concept existed.
            // For now, Admin and Editor roles are checked by [Authorize].
            
            // Serialize the layout object to string for storage
            page.Layout = System.Text.Json.JsonSerializer.Serialize(layout);
            await _repo.UpdateAsync(page);
            
            return Ok(new { success = true, message = "Layout saved successfully" });
        }
    }
}
