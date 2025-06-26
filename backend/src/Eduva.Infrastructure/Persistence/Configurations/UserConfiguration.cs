using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eduva.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(u => u.FullName)
                .HasMaxLength(100);

            builder.Property(u => u.UserName)
                .HasMaxLength(100)
                .IsRequired();
            builder.HasIndex(u => u.UserName)
                .IsUnique();

            builder.Property(u => u.AvatarUrl)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(u => u.SchoolId)
                .IsRequired(false);

            builder.Property(u => u.Status)
                .IsRequired()
                .HasConversion<string>();

            // Relationships
            builder.HasOne(u => u.School)
                   .WithMany(s => s.Users)
                   .HasForeignKey(u => u.SchoolId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.AIUsageLogs)
                     .WithOne(a => a.User)
                     .HasForeignKey(a => a.UserId)
                     .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.ClassesAsTeacher)
                    .WithOne(c => c.Teacher)
                    .HasForeignKey(c => c.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.StudentClasses)
                    .WithOne(sc => sc.Student)
                    .HasForeignKey(sc => sc.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.PersonalFolders)
                    .WithOne(f => f.User)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.CreatedLessonMaterials)
                    .WithOne(lm => lm.CreatedByUser)
                    .HasForeignKey(lm => lm.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.ApprovedLessonMaterials)
                    .WithOne(lm => lm.Approver)
                    .HasForeignKey(lm => lm.ApproverId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.CreatedLessonMaterialQuestions)
                    .WithOne(q => q.CreatedByUser)
                    .HasForeignKey(q => q.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.CreatedQuestionComments)
                    .WithOne(c => c.CreatedByUser)
                    .HasForeignKey(c => c.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.ReceivedNotifications)
                    .WithOne(un => un.TargetUser)
                    .HasForeignKey(un => un.TargetUserId)
                    .OnDelete(DeleteBehavior.Cascade);

        }
    }
}