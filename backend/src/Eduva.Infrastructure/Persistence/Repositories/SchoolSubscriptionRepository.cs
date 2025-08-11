using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class SchoolSubscriptionRepository : GenericRepository<SchoolSubscription, Guid>, ISchoolSubscriptionRepository
    {
        public SchoolSubscriptionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<SchoolSubscription?> GetActiveSubscriptionBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default)
        {
            return await _context.SchoolSubscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s =>
                    s.SchoolId == schoolId &&
                    s.SubscriptionStatus == SubscriptionStatus.Active,
                    cancellationToken);
        }

        public async Task<SchoolSubscription?> GetByPaymentTransactionIdAsync(Guid paymentTransactionId, CancellationToken cancellationToken = default)
        {
            return await _context.SchoolSubscriptions
                .Include(s => s.Plan)
                .Include(s => s.School)
                .FirstOrDefaultAsync(s => s.PaymentTransactionId == paymentTransactionId, cancellationToken);
        }

        public async Task<int> CountSchoolsUsingPlanAsync(int planId, CancellationToken cancellationToken = default)
        {
            return await _context.SchoolSubscriptions
                .CountAsync(s => s.PlanId == planId && s.SubscriptionStatus == SubscriptionStatus.Active, cancellationToken);
        }

        public async Task<SchoolSubscription?> GetLatestPaidBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default)
        {
            return await _context.SchoolSubscriptions
                .Include(s => s.Plan)
                .Include(s => s.PaymentTransaction)
                .Where(s => s.SchoolId == schoolId)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<SchoolSubscription>> GetExpiringSubscriptionsAsync(DateTimeOffset currentTime)
        {
            return await _context.SchoolSubscriptions
                .Where(s => s.SubscriptionStatus == SubscriptionStatus.Active && s.EndDate <= currentTime)
                .ToListAsync();
        }

        public async Task<SchoolSubscription?> GetLatestSubscriptionBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default)
        {
            return await _context.SchoolSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.SchoolId == schoolId)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<SchoolSubscription?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.SchoolSubscriptions
             .Include(x => x.School)
             .Include(x => x.Plan)
             .Include(x => x.PaymentTransaction)
             .ThenInclude(pt => pt.User)
             .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task UpdateSubscriptionStatusAsync(int schoolId, SubscriptionStatus status)
        {
            var subscription = await _context.SchoolSubscriptions
                .FirstOrDefaultAsync(s => s.SchoolId == schoolId);

            if (subscription != null)
            {
                subscription.SubscriptionStatus = status;
                _context.SchoolSubscriptions.Update(subscription);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new KeyNotFoundException($"No subscription found for school ID {schoolId}.");
            }
        }

        public async Task<List<SchoolSubscription>> GetSubscriptionsExpiringBetweenAsync(DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default)
        {
            return await _context.SchoolSubscriptions
                .Include(s => s.Plan)
                .Include(s => s.School)
                .Where(s => s.SubscriptionStatus == SubscriptionStatus.Active &&
                           s.EndDate >= startDate &&
                           s.EndDate <= endDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SchoolSubscription>> GetSubscriptionsExpiredOnDateAsync(DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default)
        {
            return await _context.SchoolSubscriptions
                .Include(s => s.Plan)
                .Include(s => s.School)
                .Where(s => s.SubscriptionStatus == SubscriptionStatus.Active &&
                           s.EndDate >= startDate &&
                           s.EndDate < endDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SchoolSubscription>> GetSubscriptionsExpiredBeforeAsync(DateTimeOffset beforeDate, CancellationToken cancellationToken = default)
        {
            var schoolsWithActiveSubscriptions = await _context.SchoolSubscriptions
                .Where(s => s.SubscriptionStatus == SubscriptionStatus.Active)
                .Select(s => s.SchoolId)
                .ToListAsync(cancellationToken);

            // First, get the latest expired subscription for each school without Include
            var latestExpiredSubscriptionIds = await _context.SchoolSubscriptions
                .Where(s => s.SubscriptionStatus == SubscriptionStatus.Expired &&
                           !schoolsWithActiveSubscriptions.Contains(s.SchoolId))
                .GroupBy(s => s.SchoolId)
                .Select(g => g.OrderByDescending(s => s.EndDate).First().Id)
                .ToListAsync(cancellationToken);

            // Then, get the full subscription details with Include for those IDs that meet the date criteria
            return await _context.SchoolSubscriptions
                .Include(s => s.Plan)
                .Include(s => s.School)
                .Where(s => latestExpiredSubscriptionIds.Contains(s.Id) && s.EndDate < beforeDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SchoolSubscription>> GetAllActiveSchoolSubscriptionsExceedingStorageLimitAsync
            (DateTimeOffset dataCleanupDate, CancellationToken cancellationToken = default)
        {
            return await _context.SchoolSubscriptions
                .Include(s => s.Plan)
                .Include(s => s.School)
                .Where(s => s.SubscriptionStatus == SubscriptionStatus.Active &&
                            s.StartDate < dataCleanupDate)
                .ToListAsync(cancellationToken);
        }
    }
}