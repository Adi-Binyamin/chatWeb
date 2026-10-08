using Microsoft.EntityFrameworkCore;
using chatWeb.Models;

namespace chatWeb.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ChatParticipant> ChatParticipants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatParticipant>()
                .HasKey(cp => new { cp.ChatId, cp.Username });

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatId);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.User)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.Username);

            modelBuilder.Entity<ChatParticipant>()
                .HasOne(cp => cp.Chat)
                .WithMany(c => c.Participants)
                .HasForeignKey(cp => cp.ChatId);

            modelBuilder.Entity<ChatParticipant>()
                .HasOne(cp => cp.User)
                .WithMany(u => u.ChatParticipants)
                .HasForeignKey(cp => cp.Username);
        }
    }
}
