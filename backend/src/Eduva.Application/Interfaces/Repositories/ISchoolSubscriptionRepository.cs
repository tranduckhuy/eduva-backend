using Eduva.Domain.Entities;
using Eduva.Domain.Enums;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface ISchoolSubscriptionRepository : IGenericRepository<SchoolSubscription, Guid>
    {
        Task<SchoolSubscription?> GetActiveSubscriptionBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default);
        Task<SchoolSubscription?> GetByPaymentTransactionIdAsync(Guid paymentTransactionId, CancellationToken cancellationToken = default);
        Task<int> CountSchoolsUsingPlanAsync(int planId, CancellationToken cancellationToken = default);
        Task<SchoolSubscription?> GetLatestPaidBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default);
        Task<List<SchoolSubscription>> GetExpiringSubscriptionsAsync(DateTimeOffset currentTime);
        Task<SchoolSubscription?> GetLatestSubscriptionBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default);
        Task<SchoolSubscription?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken);
        Task UpdateSubscriptionStatusAsync(int schoolId, SubscriptionStatus status);

        Task<List<SchoolSubscription>> GetAllActiveSchoolSubscriptionsExceedingStorageLimitAsync(DateTimeOffset dataCleanupDate, CancellationToken cancellationToken = default);
        Task<List<SchoolSubscription>> GetSubscriptionsExpiringBetweenAsync(DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
        Task<List<SchoolSubscription>> GetSubscriptionsExpiredOnDateAsync(DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
        Task<List<SchoolSubscription>> GetSubscriptionsExpiredBeforeAsync(DateTimeOffset beforeDate, CancellationToken cancellationToken = default);
    }
}