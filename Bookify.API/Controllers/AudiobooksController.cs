using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Bookify.API.Data;
using Bookify.API.Models;
using Bookify.API.DTOs;
using Bookify.API.Services;

namespace Bookify.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // Uncomment after configuring AD fully to protect
    public class AudiobooksController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IBlobStorageService _blobStorageService;

        public AudiobooksController(AppDbContext context, IBlobStorageService blobStorageService)
        {
            _context = context;
            _blobStorageService = blobStorageService;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(_context.Audiobooks.Include(a => a.Chapters).AsQueryable());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var audiobook = await _context.Audiobooks
                .Include(a => a.Chapters)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (audiobook == null) return NotFound();

            return Ok(audiobook);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateAudiobookDto? dto)
        {
            if (dto == null) return BadRequest(new ProblemDetails 
            { 
                Title = "Invalid Request", 
                Detail = "Request body is required." 
            });

            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> 
                { 
                    ["Title"] = new[] { "The Title field is required." } 
                }));

            if (string.IsNullOrWhiteSpace(dto.Author))
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> 
                { 
                    ["Author"] = new[] { "The Author field is required." } 
                }));

            var audiobook = new Audiobook
            {
                Title = dto.Title.Trim(),
                Author = dto.Author.Trim(),
                Category = dto.Category?.Trim() ?? string.Empty,
                Genre = dto.Genre?.Trim() ?? string.Empty,
                Description = dto.Description?.Trim() ?? string.Empty
            };

            _context.Audiobooks.Add(audiobook);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = audiobook.Id }, audiobook);
        }

        [HttpPost("{id}/cover")]
        public async Task<IActionResult> UploadCover(Guid id, IFormFile coverImage)
        {
            var audiobook = await _context.Audiobooks.FindAsync(id);
            if (audiobook == null) return NotFound("Audiobook not found.");

            if (coverImage == null || coverImage.Length == 0) return BadRequest("No image uploaded.");

            var ext = Path.GetExtension(coverImage.FileName ?? "cover");
            var blobName = $"{id}{ext}";

            using var stream = coverImage.OpenReadStream();
            var coverUrl = await _blobStorageService.UploadCoverAsync(blobName, stream, coverImage.ContentType);

            audiobook.CoverUrl = coverUrl;
            await _context.SaveChangesAsync();

            return Ok(new { CoverUrl = coverUrl });
        }

        [HttpPost("{id}/chapters")]
        public async Task<IActionResult> AddChapter(Guid id, [FromForm] CreateChapterDto? dto)
        {
            if (dto == null) return BadRequest("Request body is required.");

            var audiobook = await _context.Audiobooks.FindAsync(id);
            if (audiobook == null) return NotFound("Audiobook not found.");

            if (dto.AudioFile == null || dto.AudioFile.Length == 0) return BadRequest("No audio file uploaded.");
            if (dto.Order < 0) return BadRequest("Order must be non-negative.");

            var blobName = $"{id}/chapter-{dto.Order}-{Guid.NewGuid()}.mp3";

            using var stream = dto.AudioFile.OpenReadStream();
            await _blobStorageService.UploadAudioAsync(blobName, stream, dto.AudioFile.ContentType);

            // TODO: Extract duration using NAudio/TagLibSharp. For now we put 0 or mock
            var chapter = new Chapter
            {
                AudiobookId = id,
                Title = dto.Title,
                Order = dto.Order,
                AudioBlobName = blobName,
                DurationSeconds = 0 // Will be handled by background worker or extracted here
            };

            _context.Chapters.Add(chapter);
            await _context.SaveChangesAsync();

            return Ok(chapter);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var audiobook = await _context.Audiobooks.FindAsync(id);
            if (audiobook == null) return NotFound();

            _context.Audiobooks.Remove(audiobook);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
