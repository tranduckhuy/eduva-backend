using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eduva.Infrastructure.Persistence.Configurations
{
    public class AIServicePricingConfiguration : IEntityTypeConfiguration<AIServicePricing>
    {
        public void Configure(EntityTypeBuilder<AIServicePricing> builder)
        {
            builder.Property(asp => asp.ServiceType)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(asp => asp.PricePerMinuteCredits)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(asp => asp.Status)
                .HasConversion<string>()
                .IsRequired();
        }
    }
}
