using Eduva.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.DbContext
{
    public class AppDbContext : IdentityDbContext<
        User,
        IdentityRole<Guid>,
        Guid,
        IdentityUserClaim<Guid>,
        IdentityUserRole<Guid>,
        IdentityUserLogin<Guid>,
        IdentityRoleClaim<Guid>,
        IdentityUserToken<Guid>>
    {
        protected AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<School> Schools { get; set; } = default!;
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = default!;
        public DbSet<SchoolSubscription> SchoolSubscriptions { get; set; } = default!;
        public DbSet<AIUsageLog> AIUsageLogs { get; set; } = default!;
        public DbSet<Classroom> Classes { get; set; } = default!;
        public DbSet<StudentClass> StudentClasses { get; set; } = default!;
        public DbSet<Folder> Folders { get; set; } = default!;
        public DbSet<LessonMaterial> LessonMaterials { get; set; } = default!;
        public DbSet<FolderLessonMaterial> FolderLessonMaterials { get; set; } = default!;
        public DbSet<LessonMaterialApproval> LessonMaterialApprovals { get; set; } = default!;
        public DbSet<LessonMaterialQuestion> LessonMaterialQuestions { get; set; } = default!;
        public DbSet<QuestionComment> QuestionComments { get; set; } = default!;
        public DbSet<Notification> Notifications { get; set; } = default!;
        public DbSet<UserNotification> UserNotifications { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
