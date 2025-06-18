using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface ISchoolSubscriptionRepository : IGenericRepository<SchoolSubscription, Guid>
    {
        Task<SchoolSubscription?> FindByTransactionIdAsync(string transactionId);
    }
}