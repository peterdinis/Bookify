using Bookify.API.Data;
using Bookify.API.DTOs;
using Bookify.API.Models;
using Bookify.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bookify.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // Require user login
    public class PlaybackController(AppDbContext context, IBlobStorageService blobStorageService)
        : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly IBlobStorageService _blobStorageService = blobStorageService;

        private static Guid GetCurrentUserId()
        {
            // For now, mock a user ID until Entra ID is fully tested by user
            // In real app: return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            return Guid.Parse("00000000-0000-0000-0000-000000000001");
        }

        [HttpGet("{audiobookId}/chapter/{chapterId}/stream")]
        public async Task<IActionResult> GetStreamUrl(Guid audiobookId, Guid chapterId)
        {
            var chapter = await _context.Chapters.FirstOrDefaultAsync(c =>
                c.Id == chapterId && c.AudiobookId == audiobookId
            );
            if (chapter == null)
                return NotFound("Chapter not found.");

            // Generate SAS url valid for 2 hours
            var sasUrl = _blobStorageService.GetAudioSasUrl(
                chapter.AudioBlobName,
                TimeSpan.FromHours(2)
            );

            // Increment play count (simple approach, should ideally be done differently to avoid abuse)
            var audiobook = await _context.Audiobooks.FindAsync(audiobookId);
            if (audiobook != null)
            {
                audiobook.PlayCount++;
                await _context.SaveChangesAsync();
            }

            return Ok(new { StreamUrl = sasUrl });
        }

        [HttpPost("progress")]
        public async Task<IActionResult> SaveProgress([FromBody] SaveProgressDto? dto)
        {
            if (dto == null)
                return BadRequest("Request body is required.");

            var userId = GetCurrentUserId();

            var progress = await _context.PlaybackProgresses.FirstOrDefaultAsync(p =>
                p.UserId == userId && p.AudiobookId == dto.AudiobookId
            );

            if (progress == null)
            {
                progress = new PlaybackProgress
                {
                    UserId = userId,
                    AudiobookId = dto.AudiobookId,
                    LastChapterId = dto.ChapterId,
                    PositionSeconds = dto.PositionSeconds,
                    LastUpdated = DateTime.UtcNow,
                };
                _context.PlaybackProgresses.Add(progress);
            }
            else
            {
                progress.LastChapterId = dto.ChapterId;
                progress.PositionSeconds = dto.PositionSeconds;
                progress.LastUpdated = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(progress);
        }

        [HttpGet("progress/{audiobookId}")]
        public async Task<IActionResult> GetProgress(Guid audiobookId)
        {
            var userId = GetCurrentUserId();

            var progress = await _context
                .PlaybackProgresses.Include(p => p.LastChapter)
                .FirstOrDefaultAsync(p => p.UserId == userId && p.AudiobookId == audiobookId);

            if (progress == null)
                return NotFound();

            return Ok(progress);
        }
    }
}
