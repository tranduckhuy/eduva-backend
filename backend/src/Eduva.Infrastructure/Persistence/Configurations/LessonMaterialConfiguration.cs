using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eduva.Infrastructure.Persistence.Configurations
{
    public class LessonMaterialConfiguration : IEntityTypeConfiguration<LessonMaterial>
    {
        public void Configure(EntityTypeBuilder<LessonMaterial> builder)
        {
            builder.Property(lm => lm.CreatedByUserId)
                .IsRequired();

            builder.Property(lm => lm.Title)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(lm => lm.Description)
                .HasColumnType("text");

            builder.Property(lm => lm.ContentType)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(lm => lm.Tag)
                .HasMaxLength(100);

            builder.Property(lm => lm.LessonStatus)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(lm => lm.Duration)
                .IsRequired();

            builder.Property(lm => lm.FileSize)
                .IsRequired();

            builder.Property(lm => lm.IsAIContent)
                .IsRequired();

            builder.Property(lm => lm.SourceUrl)
                .HasMaxLength(2048);

            builder.Property(lm => lm.Visibility)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(lm => lm.CreatedByUserId)
                .IsRequired();

            // Relationships
            builder.HasOne(lm => lm.CreatedByUser)
                .WithMany(u => u.CreatedLessonMaterials)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(lm => lm.School)
                .WithMany(s => s.LessonMaterials)
                .HasForeignKey(lm => lm.SchoolId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(lm => lm.FolderLessonMaterials)
                .WithOne(flm => flm.LessonMaterial)
                .HasForeignKey(flm => flm.LessonMaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(lm => lm.LessonMaterialApprovals)
                .WithOne(lma => lma.LessonMaterial)
                .HasForeignKey(lma => lma.LessonMaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(lm => lm.LessonMaterialQuestions)
                .WithOne(lmq => lmq.LessonMaterial)
                .HasForeignKey(lmq => lmq.LessonMaterialId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}