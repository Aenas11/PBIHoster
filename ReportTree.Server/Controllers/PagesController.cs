using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportTree.Server.Models;
using ReportTree.Server.Persistance;

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
        public async Task<IEnumerable<Page>> Get()
        {
            return await _repo.GetAllAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Page>> Get(int id)
        {
            var page = await _repo.GetByIdAsync(id);
            if (page == null) return NotFound();
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
    }
}
