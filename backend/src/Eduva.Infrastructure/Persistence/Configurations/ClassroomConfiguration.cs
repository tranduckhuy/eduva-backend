using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eduva.Infrastructure.Persistence.Configurations
{
    public class ClassroomConfiguration : IEntityTypeConfiguration<Classroom>
    {
        public void Configure(EntityTypeBuilder<Classroom> builder)
        {
            builder.Property(c => c.SchoolId)
                .IsRequired();

            builder.Property(c => c.Name)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(c => c.ClassCode)
                .HasMaxLength(10);

            builder.Property(c => c.Status)
                .HasConversion<string>()
                .IsRequired();

            // Indexes
            builder.HasIndex(c => new { c.SchoolId, c.Status });

            // Relationships
            builder.HasOne(c => c.School)
                .WithMany(s => s.Classes)
                .HasForeignKey(c => c.SchoolId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.StudentClasses)
                .WithOne(sc => sc.Class)
                .HasForeignKey(sc => sc.ClassId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}