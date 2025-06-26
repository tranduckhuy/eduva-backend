using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eduva.Infrastructure.Persistence.Configurations
{
    public class LessonMaterialQuestionConfiguration : IEntityTypeConfiguration<LessonMaterialQuestion>
    {
        public void Configure(EntityTypeBuilder<LessonMaterialQuestion> builder)
        {
            builder.Property(lmq => lmq.LessonMaterialId)
                .IsRequired();

            builder.Property(lmq => lmq.Title)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(lmq => lmq.Content)
                .HasColumnType("text")
                .IsRequired();

            // Relationships
            builder.HasOne(lmq => lmq.LessonMaterial)
                .WithMany(lm => lm.LessonMaterialQuestions)
                .HasForeignKey(lmq => lmq.LessonMaterialId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(lmq => lmq.CreatedByUser)
                .WithMany(u => u.CreatedLessonMaterialQuestions)
                .HasForeignKey(lmq => lmq.CreatedByUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(lmq => lmq.Comments)
                .WithOne(qc => qc.Question)
                .HasForeignKey(qc => qc.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}