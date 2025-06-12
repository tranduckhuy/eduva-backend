using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Configurations
{
    public class SchoolSubscriptionConfiguration : IEntityTypeConfiguration<SchoolSubscription>
    {
        public void Configure(EntityTypeBuilder<SchoolSubscription> builder)
        {
            builder.ToTable("SchoolSubscription");

            builder.Property(ss => ss.SchoolId)
                .IsRequired();

            builder.Property(ss => ss.PlanId)
                .IsRequired();

            builder.Property(ss => ss.StartDate)
                .IsRequired();

            builder.Property(ss => ss.EndDate)
                .IsRequired();

            builder.Property(ss => ss.SubscriptionStatus)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(ss => ss.PaymentStatus)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(ss => ss.PaymentMethod)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(ss => ss.TransactionId)
                .HasMaxLength(255)
                .IsRequired(false); // TransactionId can be null if not applicable

            builder.Property(ss => ss.AmountPaid)
                .HasColumnType("numeric(18,2)");

            builder.Property(ss => ss.CurrentPeriodAIUsageMinutes)
                .HasColumnType("numeric(18,2)");

            builder.Property(ss => ss.LastUsageResetDate)
                .IsRequired();

            builder.Property(ss => ss.PurchasedAt)
                .IsRequired();

            // Relationships (Foreign Keys)
            builder.HasOne(ss => ss.School)
                .WithMany(s => s.SchoolSubscriptions)
                .HasForeignKey(ss => ss.SchoolId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ss => ss.Plan)
                .WithMany(sp => sp.SchoolSubscriptions)
                .HasForeignKey(ss => ss.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
