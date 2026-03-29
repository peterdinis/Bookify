using System;

namespace Bookify.API.Models
{
    public class PlaybackProgress
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }
        public User? User { get; set; }

        public Guid AudiobookId { get; set; }
        public Audiobook? Audiobook { get; set; }

        public Guid LastChapterId { get; set; }
        public Chapter? LastChapter { get; set; }

        public double PositionSeconds { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
