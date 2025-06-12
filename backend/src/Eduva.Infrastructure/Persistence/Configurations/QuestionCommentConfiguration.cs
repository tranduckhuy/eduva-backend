using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eduva.Infrastructure.Persistence.Configurations
{
    public class QuestionCommentConfiguration : IEntityTypeConfiguration<QuestionComment>
    {
        public void Configure(EntityTypeBuilder<QuestionComment> builder)
        {
            builder.Property(qc => qc.QuestionID)
                .IsRequired();

            builder.Property(qc => qc.Content)
                .HasColumnType("text")
                .IsRequired();

            builder.Property(qc => qc.CreatedBy)
                .IsRequired();

            // Relationships
            builder.HasOne(qc => qc.Question)
                .WithMany(lmq => lmq.Comments)
                .HasForeignKey(qc => qc.QuestionID)
                .OnDelete(DeleteBehavior.Restrict);

            // Self-referencing relationship for replies
            builder.HasOne(qc => qc.ParentComment)
                .WithMany(pc => pc.Replies)
                .HasForeignKey(qc => qc.ParentCommentId)
                .IsRequired(false) // Nullable
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(qc => qc.CreatedByUser)
                .WithMany(u => u.CreatedQuestionComments)
                .HasForeignKey(qc => qc.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
