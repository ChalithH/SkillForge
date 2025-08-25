using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillForge.Api.Models;

namespace SkillForge.Api.Data.Configurations;

public class CreditTransactionConfiguration : IEntityTypeConfiguration<CreditTransaction>
{
    public void Configure(EntityTypeBuilder<CreditTransaction> builder)
    {
        builder.HasKey(ct => ct.Id);

        builder.Property(ct => ct.TransactionType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ct => ct.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ct => ct.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Foreign key relationship
        builder.HasOne(ct => ct.User)
            .WithMany()
            .HasForeignKey(ct => ct.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(ct => ct.UserId);
        builder.HasIndex(ct => ct.CreatedAt);
        builder.HasIndex(ct => ct.TransactionType);
    }
}