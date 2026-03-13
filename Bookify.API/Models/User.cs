using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bookify.API.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public string EntraId { get; set; } = string.Empty;
        
        public string Role { get; set; } = "User"; // "User" or "Admin"
        
        public bool IsActive { get; set; } = true;
        
        public ICollection<PlaybackProgress> Progresses { get; set; } = new List<PlaybackProgress>();
    }
}
