using Microsoft.EntityFrameworkCore;
using SkillForge.Api.Models;

namespace SkillForge.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<UserSkill> UserSkills { get; set; }
        public DbSet<SkillExchange> SkillExchanges { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure User entity
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Configure UserSkill relationships
            modelBuilder.Entity<UserSkill>()
                .HasOne(us => us.User)
                .WithMany(u => u.UserSkills)
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserSkill>()
                .HasOne(us => us.Skill)
                .WithMany(s => s.UserSkills)
                .HasForeignKey(us => us.SkillId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure SkillExchange relationships
            modelBuilder.Entity<SkillExchange>()
                .HasOne(se => se.Offerer)
                .WithMany(u => u.OfferedExchanges)
                .HasForeignKey(se => se.OffererId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SkillExchange>()
                .HasOne(se => se.Learner)
                .WithMany(u => u.LearnedExchanges)
                .HasForeignKey(se => se.LearnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SkillExchange>()
                .HasOne(se => se.Skill)
                .WithMany()
                .HasForeignKey(se => se.SkillId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Review relationships
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Exchange)
                .WithMany(e => e.Reviews)
                .HasForeignKey(r => r.ExchangeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Reviewer)
                .WithMany(u => u.ReviewsGiven)
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.ReviewedUser)
                .WithMany(u => u.ReviewsReceived)
                .HasForeignKey(r => r.ReviewedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure value constraints
            modelBuilder.Entity<UserSkill>()
                .Property(us => us.ProficiencyLevel)
                .HasDefaultValue(1);

            modelBuilder.Entity<User>()
                .Property(u => u.TimeCredits)
                .HasDefaultValue(5);

            modelBuilder.Entity<Review>()
                .Property(r => r.Rating)
                .HasDefaultValue(1);

            // Configure default timestamps
            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<User>()
                .Property(u => u.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<SkillExchange>()
                .Property(se => se.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<SkillExchange>()
                .Property(se => se.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Review>()
                .Property(r => r.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}