using Bookify.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookify.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Audiobook> Audiobooks { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<PlaybackProgress> PlaybackProgresses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasIndex(u => u.EntraId).IsUnique();

            modelBuilder
                .Entity<PlaybackProgress>()
                .HasIndex(p => new { p.UserId, p.AudiobookId })
                .IsUnique();

            modelBuilder
                .Entity<Chapter>()
                .HasOne(c => c.Audiobook)
                .WithMany(a => a.Chapters)
                .HasForeignKey(c => c.AudiobookId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder
                .Entity<PlaybackProgress>()
                .HasOne(p => p.User)
                .WithMany(u => u.Progresses)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder
                .Entity<PlaybackProgress>()
                .HasOne(p => p.LastChapter)
                .WithMany()
                .HasForeignKey(p => p.LastChapterId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
