using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eduva.Infrastructure.Persistence.Configurations
{
    public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
    {
        public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
        {
            builder.Property(x => x.PaymentPurpose)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(x => x.RelatedId)
                .HasMaxLength(100);

            builder.Property(x => x.PaymentMethod)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(x => x.PaymentStatus)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(x => x.TransactionCode)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            // Relationships
            builder.HasOne(x => x.User)
                .WithMany(u => u.PaymentTransactions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
