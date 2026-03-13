using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Bookify.API.DTOs
{
    public class CreateAudiobookDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string Author { get; set; } = string.Empty;
        
        public string Category { get; set; } = string.Empty;
        
        public string Genre { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
    }

    public class CreateChapterDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        public int Order { get; set; }
        
        [Required]
        public IFormFile AudioFile { get; set; } = null!;
    }
}
