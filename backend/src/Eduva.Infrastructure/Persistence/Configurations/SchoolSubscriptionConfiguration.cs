using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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

            builder.Property(ss => ss.BillingCycle)
                .HasConversion<string>()
                .IsRequired();

            builder.HasIndex(ss => ss.PaymentTransactionId)
                .IsUnique();
            builder.Property(ss => ss.PaymentTransactionId)
                .IsRequired();

            // Indexes
            builder.HasIndex(builder => builder.SchoolId);

            // Relationships (Foreign Keys)
            builder.HasOne(ss => ss.School)
                .WithMany(s => s.SchoolSubscriptions)
                .HasForeignKey(ss => ss.SchoolId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ss => ss.Plan)
                .WithMany(sp => sp.SchoolSubscriptions)
                .HasForeignKey(ss => ss.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ss => ss.PaymentTransaction)
                .WithMany()
                .HasForeignKey(ss => ss.PaymentTransactionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}