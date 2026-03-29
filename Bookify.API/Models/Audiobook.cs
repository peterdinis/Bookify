using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bookify.API.Models
{
    public class Audiobook
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Author { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Genre { get; set; } = string.Empty;

        public string CoverUrl { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsApproved { get; set; } = false;

        public int PlayCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
    }
}
