using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Bookify.API.Data;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Bookify.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize(Roles = "Admin")] // Uncomment when EntraID is fully tested
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("audiobooks/{id}/approve")]
        public async Task<IActionResult> ApproveAudiobook(Guid id, [FromQuery] bool approve = true)
        {
            var audiobook = await _context.Audiobooks.FindAsync(id);
            if (audiobook == null) return NotFound("Audiobook not found.");

            audiobook.IsApproved = approve;
            await _context.SaveChangesAsync();

            return Ok(new { id = audiobook.Id, isApproved = audiobook.IsApproved });
        }

        [HttpPost("users/{userId}/deactivate")]
        public async Task<IActionResult> DeactivateUser(Guid userId, [FromQuery] bool deactivate = true)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");

            user.IsActive = !deactivate;
            await _context.SaveChangesAsync();

            return Ok(new { id = user.Id, isActive = user.IsActive });
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalAudiobooks = await _context.Audiobooks.CountAsync();
            var totalPlays = await _context.Audiobooks.SumAsync(a => a.PlayCount);

            return Ok(new
            {
                TotalUsers = totalUsers,
                TotalAudiobooks = totalAudiobooks,
                TotalPlays = totalPlays
            });
        }
    }
}
