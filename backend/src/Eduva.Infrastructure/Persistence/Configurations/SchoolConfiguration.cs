using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eduva.Infrastructure.Persistence.Configurations
{
    public class SchoolConfiguration : IEntityTypeConfiguration<School>
    {
        public void Configure(EntityTypeBuilder<School> builder)
        {
            builder.Property(s => s.Name).HasMaxLength(255).IsRequired();

            builder.Property(s => s.Code).HasMaxLength(50).IsRequired();
            builder.HasIndex(s => s.Code).IsUnique();

            builder.Property(s => s.ContactEmail).HasMaxLength(100).IsRequired();
            builder.HasIndex(s => s.ContactEmail).IsUnique();

            builder.Property(s => s.ContactPhone).HasMaxLength(100).IsRequired();

            builder.Property(s => s.WebsiteUrl).HasMaxLength(255);

            builder.Property(s => s.Status).HasConversion<string>().IsRequired();

            builder.Property(s => s.CreatedAt).IsRequired();

            // Relationships
            builder.HasMany(s => s.Users)
                   .WithOne(u => u.School)
                   .HasForeignKey(u => u.SchoolId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.Classes)
                   .WithOne(c => c.School)
                   .HasForeignKey(c => c.SchoolId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.LessonMaterials)
                   .WithOne(lm => lm.School)
                   .HasForeignKey(lm => lm.SchoolId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.SchoolSubscriptions)
                    .WithOne(ss => ss.School)
                    .HasForeignKey(ss => ss.SchoolId)
                    .OnDelete(DeleteBehavior.Cascade);
        }
    }
}