using System;
using System.ComponentModel.DataAnnotations;

namespace Bookify.API.DTOs
{
    public class SaveProgressDto
    {
        [Required]
        public Guid AudiobookId { get; set; }
        
        [Required]
        public Guid ChapterId { get; set; }
        
        [Required]
        [Range(0, double.MaxValue)]
        public double PositionSeconds { get; set; }
    }
}
