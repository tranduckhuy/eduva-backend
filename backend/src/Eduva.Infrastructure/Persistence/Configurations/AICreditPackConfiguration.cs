using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eduva.Infrastructure.Persistence.Configurations
{
    public class AICreditPackConfiguration : IEntityTypeConfiguration<AICreditPack>
    {
        public void Configure(EntityTypeBuilder<AICreditPack> builder)
        {
            builder.Property(sc => sc.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Credits)
                .IsRequired()
                .HasDefaultValue(0);


            builder.Property(x => x.Price)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.BonusCredits)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .IsRequired();

            // Relationships
            builder.HasMany(x => x.UserCreditTransactions)
                .WithOne()
                .HasForeignKey(uct => uct.AICreditPackId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
