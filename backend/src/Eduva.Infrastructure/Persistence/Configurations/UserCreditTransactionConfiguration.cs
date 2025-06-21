using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eduva.Infrastructure.Persistence.Configurations
{
    public class UserCreditTransactionConfiguration : IEntityTypeConfiguration<UserCreditTransaction>
    {
        public void Configure(EntityTypeBuilder<UserCreditTransaction> builder)
        {
            builder.Property(uct => uct.UserId)
                .IsRequired();

            builder.Property(uct => uct.PaymentTransactionId)
                .IsRequired();

            builder.Property(uct => uct.AICreditPackId)
                .IsRequired();

            builder.Property(uct => uct.Credits)
                .IsRequired();

            // Relationships
            builder.HasOne(uct => uct.User)
                .WithMany(u => u.CreditTransactions)
                .HasForeignKey(uct => uct.UserId);
            builder.HasOne(uct => uct.AICreditPack)
                .WithMany(aicp => aicp.UserCreditTransactions)
                .HasForeignKey(uct => uct.AICreditPackId);
            builder.HasOne(uct => uct.PaymentTransaction)
                .WithMany()
                .HasForeignKey(uct => uct.PaymentTransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
