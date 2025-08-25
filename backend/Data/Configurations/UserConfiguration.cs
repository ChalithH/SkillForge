using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillForge.Api.Models;

namespace SkillForge.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.Bio)
            .HasMaxLength(2000);

        builder.Property(u => u.ProfileImageUrl)
            .HasMaxLength(500);

        // Database-agnostic timestamp configuration
        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql(GetUtcNowSql());

        builder.Property(u => u.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql(GetUtcNowSql());

        builder.HasIndex(u => u.Email)
            .IsUnique();

        // Navigation properties
        builder.HasMany(u => u.UserSkills)
            .WithOne(us => us.User)
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.OfferedExchanges)
            .WithOne(se => se.Offerer)
            .HasForeignKey(se => se.OffererId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.LearnedExchanges)
            .WithOne(se => se.Learner)
            .HasForeignKey(se => se.LearnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.ReviewsGiven)
            .WithOne(r => r.Reviewer)
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.ReviewsReceived)
            .WithOne(r => r.ReviewedUser)
            .HasForeignKey(r => r.ReviewedUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static string GetUtcNowSql()
    {
        // This will be replaced by provider-specific implementations
        return "CURRENT_TIMESTAMP";
    }
}