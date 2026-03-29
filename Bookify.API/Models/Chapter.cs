using System;
using System.ComponentModel.DataAnnotations;

namespace Bookify.API.Models
{
    public class Chapter
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid AudiobookId { get; set; }
        public Audiobook? Audiobook { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string AudioBlobName { get; set; } = string.Empty;

        public double DurationSeconds { get; set; }

        public int Order { get; set; }
    }
}
