using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportTree.Server.Persistance;

namespace ReportTree.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DirectoryController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly IGroupRepository _groupRepo;

        public DirectoryController(IUserRepository userRepo, IGroupRepository groupRepo)
        {
            _userRepo = userRepo;
            _groupRepo = groupRepo;
        }

        [HttpGet("users")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query)
        {
            var users = await _userRepo.SearchAsync(query);
            return Ok(users.Select(u => new { u.Username }));
        }

        [HttpGet("groups")]
        public async Task<IActionResult> SearchGroups([FromQuery] string query)
        {
            var groups = await _groupRepo.SearchAsync(query);
            return Ok(groups.Select(g => new { g.Name }));
        }
    }
}
