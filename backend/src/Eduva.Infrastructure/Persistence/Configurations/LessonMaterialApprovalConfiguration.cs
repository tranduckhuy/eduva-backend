using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eduva.Infrastructure.Persistence.Configurations
{
    public class LessonMaterialApprovalConfiguration : IEntityTypeConfiguration<LessonMaterialApproval>
    {
        public void Configure(EntityTypeBuilder<LessonMaterialApproval> builder)
        {

            builder.Property(lma => lma.ApproverId)
                .IsRequired();

            builder.Property(lma => lma.LessonMaterialId)
                .IsRequired();

            builder.Property(lma => lma.StatusChangeTo)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(lma => lma.Feedback)
                .HasColumnType("text");

            // Relationships
            builder.HasOne(lma => lma.Approver)
                .WithMany(u => u.ApprovedLessonMaterials)
                .HasForeignKey(lma => lma.ApproverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}