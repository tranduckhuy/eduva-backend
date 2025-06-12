using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eduva.Infrastructure.Persistence.Configurations
{
    public class AIUsageLogConfiguration : IEntityTypeConfiguration<AIUsageLog>
    {
        public void Configure(EntityTypeBuilder<AIUsageLog> builder)
        {
            builder.Property(aul => aul.UserId)
                .IsRequired();

            builder.Property(aul => aul.LessonTitleAtCreation)
                .HasMaxLength(255);

            builder.Property(aul => aul.ContentType)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(aul => aul.CostMinutes)
                .IsRequired();


            // Relationships
            builder.HasOne(aul => aul.User)
                .WithMany(u => u.AIUsageLogs)
                .HasForeignKey(aul => aul.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
