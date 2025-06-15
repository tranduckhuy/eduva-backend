using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eduva.Infrastructure.Persistence.Configurations
{
    public class FolderConfiguration : IEntityTypeConfiguration<Folder>
    {
        public void Configure(EntityTypeBuilder<Folder> builder)
        {
            builder.Property(f => f.UserId).IsRequired(false);
            builder.Property(f => f.ClassId).IsRequired(false);

            builder.Property(f => f.Name)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(f => f.OwnerType)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(f => f.Order)
                .IsRequired();

            // Relationships
            // UserID is nullable, so IsRequired(false)
            builder.HasOne(f => f.User)
                .WithMany(u => u.PersonalFolders)
                .HasForeignKey(f => f.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // ClassID is nullable, so IsRequired(false)
            builder.HasOne(f => f.Class)
                .WithMany(c => c.ClassFolders)
                .HasForeignKey(f => f.ClassId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(f => f.FolderLessonMaterials)
                .WithOne(flm => flm.Folder)
                .HasForeignKey(flm => flm.FolderID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}