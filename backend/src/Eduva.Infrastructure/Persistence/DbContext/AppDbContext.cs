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
        public DbSet<SystemConfig> SystemConfigs { get; set; } = default!;
        public DbSet<Job> Jobs { get; set; } = default!;

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps<Guid>();
            UpdateTimestamps<int>();
            UpdateApplicationUserTimestamps();

            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps<TKey>()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseTimestampedEntity<TKey> &&
                            (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (BaseTimestampedEntity<TKey>)entry.Entity;
                if (entry.State == EntityState.Modified)
                {
                    entity.LastModifiedAt = DateTimeOffset.UtcNow;
                }
            }
        }

        private void UpdateApplicationUserTimestamps()
        {
            var userEntries = ChangeTracker.Entries()
                .Where(e => e.Entity is ApplicationUser &&
                            (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in userEntries)
            {
                var user = (ApplicationUser)entry.Entity;
                if (entry.State == EntityState.Modified)
                {
                    user.LastModifiedAt = DateTimeOffset.UtcNow;
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}