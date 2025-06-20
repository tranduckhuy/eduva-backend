using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface ISchoolSubscriptionRepository : IGenericRepository<SchoolSubscription, Guid>
    {
        Task<SchoolSubscription?> FindByTransactionIdAsync(string transactionId);
        Task<SchoolSubscription?> GetActiveSubscriptionBySchoolIdAsync(int schoolId);
        Task<int> CountSchoolsUsingPlanAsync(int planId, CancellationToken cancellationToken = default);
        Task<SchoolSubscription?> GetLatestPaidBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default);
    }
}