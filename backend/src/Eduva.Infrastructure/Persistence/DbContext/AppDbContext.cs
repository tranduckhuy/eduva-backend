using Eduva.Domain.Common;
using Eduva.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.DbContext
{
    public class AppDbContext : IdentityDbContext<
        ApplicationUser,
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
        public DbSet<AIUsageLog> AIUsageLogs { get; set; } = default!;
        public DbSet<UserCreditTransaction> UserCreditTransactions { get; set; } = default!;
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = default!;
        public DbSet<AICreditPack> AICreditPacks { get; set; } = default!;
        public DbSet<AIServicePricing> AIServicePricings { get; set; } = default!;

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseTimestampedEntity<Guid> &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (BaseTimestampedEntity<Guid>)entry.Entity;

                if (entry.State == EntityState.Modified)
                {
                    entity.LastModifiedAt = DateTimeOffset.UtcNow;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}