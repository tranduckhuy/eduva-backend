using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Configurations
{
    public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
    {
        public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
        {
            builder.Property(sp => sp.Name)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(sp => sp.Description)
                .HasColumnType("text");

            builder.Property(sp => sp.MaxUsers)
                .IsRequired();

            builder.Property(sp => sp.StorageLimitGB)
                .IsRequired();

            builder.Property(sp => sp.MaxMinutesPerMonth)
                .HasColumnType("numeric(18,2)") // decimal
                .IsRequired();

            builder.Property(sp => sp.PriceMonthly)
                .HasColumnType("numeric(18,2)") // decimal
                .IsRequired();

            builder.Property(sp => sp.PricePerYear)
                .HasColumnType("numeric(18,2)") // decimal
                .IsRequired();

            // Relationships
            builder.HasMany(sp => sp.SchoolSubscriptions)
                .WithOne(ss => ss.Plan)
                .HasForeignKey(ss => ss.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
