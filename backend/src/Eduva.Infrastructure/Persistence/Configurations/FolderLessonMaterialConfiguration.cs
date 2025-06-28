using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eduva.Infrastructure.Persistence.Configurations
{
    public class FolderLessonMaterialConfiguration : IEntityTypeConfiguration<FolderLessonMaterial>
    {
        public void Configure(EntityTypeBuilder<FolderLessonMaterial> builder)
        {
            builder.Property(flm => flm.FolderId)
                .IsRequired();
            builder.Property(flm => flm.LessonMaterialId)
                .IsRequired();

            builder.HasIndex(f => new { f.FolderId, f.LessonMaterialId })
                .IsUnique();

            // Relationships
            builder.HasOne(flm => flm.Folder)
                .WithMany(f => f.FolderLessonMaterials)
                .HasForeignKey(flm => flm.FolderId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(flm => flm.LessonMaterial)
                .WithMany(lm => lm.FolderLessonMaterials)
                .HasForeignKey(flm => flm.LessonMaterialId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
