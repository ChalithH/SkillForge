using Microsoft.EntityFrameworkCore;
using SkillForge.Api.Data.Configurations;
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
        public DbSet<CreditTransaction> CreditTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Apply configurations - this approach is more maintainable
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new SkillConfiguration());
            modelBuilder.ApplyConfiguration(new CreditTransactionConfiguration());
            
            // Provider-specific timestamp configurations
            ConfigureProviderSpecificFeatures(modelBuilder);
            
            // Configure UserSkill relationships and constraints
            ConfigureUserSkillEntity(modelBuilder);
            
            // Configure SkillExchange relationships
            ConfigureSkillExchangeEntity(modelBuilder);
            
            // Configure Review relationships
            ConfigureReviewEntity(modelBuilder);
        }
        
        private void ConfigureProviderSpecificFeatures(ModelBuilder modelBuilder)
        {
            var provider = Database.ProviderName;
            string utcNowSql;
            
            if (provider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                utcNowSql = "GETUTCDATE()";
            }
            else if (provider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                utcNowSql = "NOW() AT TIME ZONE 'UTC'";
            }
            else if (provider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                utcNowSql = "datetime('now')";
            }
            else
            {
                // Fallback for other providers
                utcNowSql = "CURRENT_TIMESTAMP";
            }
            
            // Apply UTC timestamp defaults
            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql(utcNowSql);

            modelBuilder.Entity<User>()
                .Property(u => u.UpdatedAt)
                .HasDefaultValueSql(utcNowSql);

            modelBuilder.Entity<SkillExchange>()
                .Property(se => se.CreatedAt)
                .HasDefaultValueSql(utcNowSql);

            modelBuilder.Entity<SkillExchange>()
                .Property(se => se.UpdatedAt)
                .HasDefaultValueSql(utcNowSql);

            modelBuilder.Entity<Review>()
                .Property(r => r.CreatedAt)
                .HasDefaultValueSql(utcNowSql);

            modelBuilder.Entity<CreditTransaction>()
                .Property(ct => ct.CreatedAt)
                .HasDefaultValueSql(utcNowSql);
        }

        private void ConfigureUserSkillEntity(ModelBuilder modelBuilder)
        {
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

            // Configure value constraints
            modelBuilder.Entity<UserSkill>()
                .Property(us => us.ProficiencyLevel)
                .HasDefaultValue(1);
            
            // Optimize matching queries with indexes
            modelBuilder.Entity<UserSkill>()
                .HasIndex(us => new { us.IsOffering, us.SkillId })
                .HasDatabaseName("IX_UserSkill_IsOffering_SkillId");
            
            modelBuilder.Entity<UserSkill>()
                .HasIndex(us => new { us.UserId, us.IsOffering })
                .HasDatabaseName("IX_UserSkill_UserId_IsOffering");
        }

        private void ConfigureSkillExchangeEntity(ModelBuilder modelBuilder)
        {
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
        }

        private void ConfigureReviewEntity(ModelBuilder modelBuilder)
        {
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

            modelBuilder.Entity<Review>()
                .Property(r => r.Rating)
                .HasDefaultValue(1);
                
            modelBuilder.Entity<Review>()
                .HasIndex(r => r.ReviewedUserId)
                .HasDatabaseName("IX_Review_ReviewedUserId");
        }
    }
}