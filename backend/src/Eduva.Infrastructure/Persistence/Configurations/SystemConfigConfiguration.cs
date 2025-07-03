using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eduva.Infrastructure.Persistence.Configurations
{
    public class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
    {
        public void Configure(EntityTypeBuilder<SystemConfig> builder)
        {
            builder.Property(sc => sc.Id).ValueGeneratedOnAdd();

            builder.Property(sc => sc.Key).IsRequired().HasMaxLength(100);
            builder.Property(sc => sc.Value).IsRequired().HasMaxLength(500);
            builder.Property(sc => sc.Description).HasMaxLength(255);
        }
    }
}
